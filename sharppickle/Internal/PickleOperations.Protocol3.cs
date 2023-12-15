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
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinaryBytes)]
    public static void PushBytes(PickleReaderState state) {
        var length = state.Stream.ReadInt32LittleEndian();
        ReadOnlyMemory<byte> buffer = state.Stream.ReadMemory(length);
        state.Stack.Push(buffer);
    }

    /// <summary>
    ///     Reads the length of the data (as a <see langword="byte" />), then reads the amount of bytes from the stream and
    ///     pushes the byte array to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.ShortBinaryBytes)]
    public static void PushShortBytes(PickleReaderState state) {
        // Read byte as length prefix.
        var length = state.Stream.ReadByte();
        if (length == -1)
            throw new UnpicklingException("EOF reached.");
        // Read number of bytes and push them to the stack.
        ReadOnlyMemory<byte> buffer = state.Stream.ReadMemory(length);
        state.Stack.Push(buffer);
    }
}
