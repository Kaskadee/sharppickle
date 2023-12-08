using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using sharppickle.Exceptions;

namespace sharppickle.Extensions;

/// <summary>
///     Provides extension methods to simplify retrieving data from a <seealso cref="Stream" />.
/// </summary>
internal static class StreamExtensions {
    /// <summary>
    ///     Reads a string from the specified stream until a new line character (\n) has been read.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the string from.</param>
    /// <returns>The string read from the specified <see cref="Stream" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadLine(this Stream stream) {
        StringBuilder sb = new();
        int b;
        while ((b = stream.ReadByte()) is not (-1 or (byte)'\n'))
            sb.Append(b);
        return sb.ToString();
    }

    /// <summary>
    ///     Reads an escaped unicode string from the current <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the string from.</param>
    /// <returns>The read string with escaped characters.</returns>
    public static string ReadUnicodeString(this Stream stream) {
        var str = stream.ReadLine();
        var regex = new Regex(@"[^\x00-\x7F]", RegexOptions.Compiled);
        return regex.Replace(str, c => $@"\u{(int)c.Value[0]:x4}");
    }

    /// <summary>
    ///     Reads a little-endian unsigned 16-bit integer from the current <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the integer from.</param>
    /// <returns>The unsigned 16-bit integer read from the stream.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16LittleEndian(this Stream stream) {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        var bytesRead = stream.Read(buffer);
        return bytesRead == buffer.Length ? BinaryPrimitives.ReadUInt16LittleEndian(buffer) : throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
    }

    /// <summary>
    ///     Reads a little-endian signed 32-bit integer from the current <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the integer from.</param>
    /// <returns>The signed 32-bit integer read from the stream.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32LittleEndian(this Stream stream) {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        var bytesRead = stream.Read(buffer);
        return bytesRead == buffer.Length ? BinaryPrimitives.ReadInt32LittleEndian(buffer) : throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
    }

    /// <summary>
    ///     Reads a little-endian unsigned 32-bit integer from the current <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the integer from.</param>
    /// <returns>The unsigned 32-bit integer read from the stream.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32LittleEndian(this Stream stream) {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        var bytesRead = stream.Read(buffer);
        return bytesRead == buffer.Length ? BinaryPrimitives.ReadUInt32LittleEndian(buffer) : throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
    }

    /// <summary>
    ///     Reads a little-endian signed 64-bit integer from the current <see cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the integer from.</param>
    /// <returns>The signed 64-bit integer read from the stream.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadInt64LittleEndian(this Stream stream) {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        var bytesRead = stream.Read(buffer);
        return bytesRead == buffer.Length ? BinaryPrimitives.ReadInt64LittleEndian(buffer) : throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
    }

    /// <summary>
    ///     Reads the specified number of bytes from the current <see cref="Stream" /> and returns the result as an instance of
    ///     <see cref="ReadOnlyMemory{T}" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="length">The length of the data to read.</param>
    /// <returns>The data read from the stream as an instance of <see cref="ReadOnlyMemory{T}" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyMemory<byte> ReadMemory(this Stream stream, long length) {
        // Validate the specified length to prevent invalid allocations.
        switch (length) {
            case 0:
                return ReadOnlyMemory<byte>.Empty;
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(length), length, "The specified length must be equal or greather than zero.");
        }

        Memory<byte> buffer = new byte[length];
        var bytesRead = stream.Read(buffer.Span);
        return bytesRead == buffer.Length ? buffer : throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
    }

    /// <summary>
    ///     Reads the specified number of bytes from the current <see cref="Stream" /> and returns the result as an instance of
    ///     <see cref="ReadOnlySpan{T}" />.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="length">The length of the data to read.</param>
    /// <returns>The data read from the stream as an instance of <see cref="ReadOnlySpan{T}" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> ReadSpan(this Stream stream, long length) {
        // Validate the specified length to prevent invalid allocations.
        switch (length) {
            case 0:
                return ReadOnlySpan<byte>.Empty;
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(length), length, "The specified length must be equal or greather than zero.");
        }

        Span<byte> buffer = new byte[length];
        var bytesRead = stream.Read(buffer);
        return bytesRead == buffer.Length ? buffer : throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
    }
}
