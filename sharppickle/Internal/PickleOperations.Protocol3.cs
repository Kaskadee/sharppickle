using System.Collections;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal;

/// <summary>
///     Provides the implementation of all op-codes defined in protocol version 3.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    ///     Reads the length of the data, then reads the amount of bytes from the stream and pushes the byte array to the top
    ///     of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinaryBytes)]
    public static void PushBytes(Stack stack, Stream stream) {
        var length = stream.ReadInt32LittleEndian();
        ReadOnlyMemory<byte> buffer = stream.ReadMemory(length);
        stack.Push(buffer);
    }

    /// <summary>
    ///     Reads the length of the data (as a <see langword="byte" />), then reads the amount of bytes from the stream and
    ///     pushes the byte array to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.ShortBinaryBytes)]
    public static void PushShortBytes(Stack stack, Stream stream) {
        // Read byte as length prefix.
        var length = stream.ReadByte();
        if (length == -1)
            throw new UnpicklingException("EOF reached.");
        // Read number of bytes and push them to the stack.
        ReadOnlyMemory<byte> buffer = stream.ReadMemory(length);
        stack.Push(buffer);
    }
}
