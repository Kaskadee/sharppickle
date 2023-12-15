using System.Collections;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal;

/// <summary>
///     Provides the implementation of all op-codes defined in protocol version 5.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    ///     Pushes a byte array to the stack with the length read from the stream and the contents read from the out-of-band
    ///     stream.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.ByteArray8)]
    public static void PushByteArray8(PickleReaderState state) {
        // Check if out-of-band stream is available.
        if (state.Reader.GetOutOfBandStream() is not { } outOfBandStream)
            throw new UnpicklingException("Out-of-band stream not available.");
        // Read length of data to read from out-of-band stream to buffer.
        var length = state.Stream.ReadInt64LittleEndian();
        ReadOnlyMemory<byte> data = outOfBandStream.ReadMemory(length);
        state.Stack.Push(data);
    }
}
