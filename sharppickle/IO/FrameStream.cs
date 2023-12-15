// sharppickle - FrameStream.cs
// Copyright (C) 2023 Fabian Creutz.
// 
// Licensed under the EUPL, Version 1.2 or – as soon they will be approved by the
// European Commission - subsequent versions of the EUPL (the "Licence");
// 
// You may not use this work except in compliance with the Licence.
// You may obtain a copy of the Licence at:
// 
// https://joinup.ec.europa.eu/software/page/eupl
// 
// Unless required by applicable law or agreed to in writing, software distributed under the Licence is distributed on an "AS IS" basis,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the Licence for the specific language governing permissions and limitations under the Licence.

using System.Buffers;

namespace sharppickle.IO;

/// <summary>
/// Provides a <see cref="Stream"/> implementation that serves as a wrapper around another <see cref="Stream"/>,
/// which allows to read data frame-wise and preventing reads outside the boundary of such frame.
/// </summary>
/// <remarks>This implementation acts as a read-only wrapper around the underlying stream.</remarks>
public sealed class FrameStream : Stream {
    private readonly Stream stream;
    private IMemoryOwner<byte>? currentFrame;
    private int currentFrameSize;
    private long currentFramePosition;
    private long currentFrameIndex;
    private readonly bool leaveOpen;
    private int isDisposed;

    /// <summary>
    /// Gets a value indicating whether the current stream supports reading.
    /// </summary>
    /// <returns><see langword="true" /> if the stream supports reading; otherwise, <see langword="false" />.</returns>
    public override bool CanRead => this.isDisposed != 1;

    /// <summary>
    /// Gets a value indicating whether the current stream supports seeking.
    /// </summary>
    /// <returns><see langword="true" /> if the stream supports seeking; otherwise, <see langword="false" />.</returns>
    public override bool CanSeek => this.isDisposed != 1;

    /// <summary>
    /// Gets a value indicating whether the current stream supports writing.
    /// </summary>
    /// <returns><c>false</c>, since the current implementation does not support writing.</returns>
    public override bool CanWrite => false;

    /// <summary>
    /// Gets the length in bytes of the stream.
    /// </summary>
    /// <returns>A long value representing the length of the stream in bytes.</returns>
    public override long Length => this.stream.Length;
    
    /// <summary>
    /// Gets or sets the position within the current stream.
    /// </summary>
    /// <returns>The current position within the stream.</returns>
    public override long Position {
        get {
            ObjectDisposedException.ThrowIf(!this.CanSeek, this);
            return this.currentFrame is not null ? this.currentFramePosition + this.currentFrameIndex : this.stream.Position;
        }
        set => this.Seek(value, SeekOrigin.Begin);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameStream"/> class.
    /// </summary>
    /// <param name="stream">The underlying <see cref="Stream"/> to encapsulate.</param>
    /// <param name="leaveOpen"><c>true</c>, to prevent the underlying stream from being disposed when this instance is disposed.</param>
    public FrameStream(Stream stream, bool leaveOpen = false) {
        ArgumentNullException.ThrowIfNull(stream);
        this.stream = stream;
        this.leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Sets the position within the current stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
    /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current stream.</returns>
    public override long Seek(long offset, SeekOrigin origin) {
        ObjectDisposedException.ThrowIf(this.isDisposed == 1, this);
        if (this.currentFrame is not null) {
            // Calculate the actual position to seek to.
            var finalIndex = origin switch {
                SeekOrigin.Begin => offset,
                SeekOrigin.End => this.Length - offset,
                var _ => this.Position + offset
            };
            
            // Check if the requested position is within the current frame boundaries.
            if(finalIndex < this.currentFramePosition || finalIndex > this.currentFramePosition + this.currentFrameSize)
                throw new InvalidOperationException("Cannot seek outside of current frame boundaries.");
            
            this.currentFrameIndex = finalIndex - this.currentFramePosition;
            this.stream.Seek(this.currentFramePosition + this.currentFrameSize, SeekOrigin.Begin);
            return finalIndex;
        }

        // If not currently in a frame, perform seek directly on the underlying stream.
        return this.stream.Seek(offset, origin);
    }

    /// <summary>
    /// Reads a sequence of bytes from the current stream or from the current frame and advances the position within the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if <paramref name="count" /> is 0 or the end of the stream has been reached.</returns>
    public override int Read(byte[] buffer, int offset, int count) {
        ObjectDisposedException.ThrowIf(this.isDisposed == 1, this);
        if (this.currentFrame is not null) {
            // Check if the frame has enough data remaining.
            var remainingDataInFrame = this.currentFrameSize - this.currentFrameIndex;
            if (count > remainingDataInFrame)
                throw new InvalidOperationException("The requested length exceeds the current frame boundaries.");
            // Copy data from the buffered frame to the output buffer.
            checked {
                Memory<byte> outputSlice = buffer.AsMemory().Slice(offset, count);
                ReadOnlyMemory<byte> dataSlice = this.currentFrame.Memory[..this.currentFrameSize].Slice((int)this.currentFrameIndex, count);
                dataSlice.CopyTo(outputSlice);
                this.currentFrameIndex += count;
            }
            // Close the frame if the end of the frame has been reached.
            if (this.currentFrameIndex == this.currentFrameSize) {
                this.currentFrame.Dispose();
                this.currentFrame = null;
                this.currentFrameIndex = 0;
                this.currentFramePosition = 0;
                this.currentFrameSize = 0;
            }
            return count;
        }

        // If not currently in a frame, read data directly from the underlying stream.
        return this.stream.Read(buffer, offset, count);
    }

    /// <summary>
    /// Reads a frame with the specified length.
    /// </summary>
    /// <param name="length">The length of the frame to read.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Cannot read frame while current frame is still active.
    /// - or -
    /// The specified length exceeds the length of the remaining data in the stream.
    /// </exception>
    /// <exception cref="NotSupportedException">The specified length exceeds the maximum supported length.</exception>
    public void ReadFrame(long length) {
        ObjectDisposedException.ThrowIf(this.isDisposed == 1, this);
        if (this.currentFrame is not null)
            throw new InvalidOperationException("Cannot read frame while current frame is still active.");
        if (length > int.MaxValue)
            throw new NotSupportedException($"The specified length ({length}) exceeds the maximum supported length ({int.MaxValue}).");
        var remainingData = this.Length - this.Position;
        if (length > remainingData)
            throw new InvalidOperationException("The specified length exceeds the length of the remaining data in the stream.");
        
        this.currentFramePosition = this.Position;
        // Allocate new buffer for the frame, if not yet allocated or the current buffer is too small.
        if (this.currentFrame is null || this.currentFrame.Memory.Length < length) {
            this.currentFrame?.Dispose();
            this.currentFrameSize = (int)length;
            this.currentFrame = MemoryPool<byte>.Shared.Rent(this.currentFrameSize);
        }
        
        // Read exactly the specified number of bytes.
        this.stream.ReadExactly(this.currentFrame.Memory[..this.currentFrameSize].Span);
    }
    
    /// <summary>
    /// Writes data from the specified buffer to the stream.
    /// </summary>
    /// <exception cref="NotSupportedException">This stream does not support writing.</exception>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("This stream does not support writing.");

    /// <summary>
    /// Sets the length of the current stream.
    /// </summary>
    /// <param name="value">The desired length of the current stream in bytes.</param>
    /// <exception cref="NotSupportedException">The stream does not support writing.</exception>
    public override void SetLength(long value) => throw new NotSupportedException("This stream does not support writing.");

    /// <summary>
    /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    /// </summary>
    /// <remarks>This method is a no-op in this implementation.</remarks>
    public override void Flush() { }
    
    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Stream" /> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing) {
        if (Interlocked.Exchange(ref this.isDisposed, 1) == 1)
            return;
        this.currentFrame?.Dispose();
        if(!this.leaveOpen)
            this.stream.Dispose();
    }
}
