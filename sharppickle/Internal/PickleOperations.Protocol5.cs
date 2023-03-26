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
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the length of the data from.</param>
    /// <param name="pickleReader">The <see cref="PickleReader" /> to retrieve the out-of-band stream.</param>
    [PickleMethod(PickleOpCodes.ByteArray8)]
    public static void PushByteArray8(Stack stack, Stream stream, PickleReader pickleReader) {
        // Check if out-of-band stream is available.
        if (pickleReader.GetOutOfBandStream() is not { } outOfBandStream)
            throw new UnpicklingException("Out-of-band stream not available.");
        // Read length of data to read from out-of-band stream to buffer.
        var length = stream.ReadInt64LittleEndian();
        ReadOnlyMemory<byte> data = outOfBandStream.ReadMemory(length);
        stack.Push(data);
    }
}
