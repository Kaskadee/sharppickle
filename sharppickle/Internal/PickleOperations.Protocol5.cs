using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal;

/// <summary>
/// Provides the implementation of all op-codes defined in protocol version 5.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    /// Pushes the next buffer (as a <see cref="Memory{T}"/> of bytes) from the provided out-of-band data to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.NextBuffer)]
    public static void PushNextBuffer(PickleReaderState state) {
        // Get next buffer from the out-of-band buffer collection.
        Memory<byte> buffer = state.Reader.GetNextBuffer();
        state.Stack.Push(buffer);
    }

    /// <summary>
    /// Pushes a read-only buffer (as a <see cref="ReadOnlyMemory{T}"/> of bytes) by converting the buffer at the top of the stack to a read-only buffer.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.ReadonlyBuffer)]
    public static void PushReadonlyBuffer(PickleReaderState state) {
        // If buffer is already read-only, skip this operation.
        if (state.Stack.Peek() is ReadOnlyMemory<byte>)
            return;
        if (state.Stack.Pop() is not Memory<byte> buffer)
            throw new UnpicklingException($"Expected buffer of type {typeof(Memory<byte>).Name} on top of the stack, but got {state.Stack.Peek()?.GetType().Name}");
        state.Stack.Push((ReadOnlyMemory<byte>)buffer);
    }
    
    /// <summary>
    /// Pushes a byte array (as a <see cref="Memory{T}"/> of bytes) to the stack by reading the length and data from the stream.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.ByteArray8)]
    public static void PushByteArray8(PickleReaderState state) {
        // Read length of data to read from the stream to buffer.
        var length = state.Stream.ReadInt64LittleEndian();
        Memory<byte> buffer = new byte[length];
        state.Stream.ReadExactly(buffer.Span);
    }
}
