using System.Buffers.Binary;
using System.Collections;
using System.Globalization;
using System.Text;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal; 

/// <summary>
/// Provides the implementation of all op-codes defined in protocol version 2.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    /// Creates a single tuple from the top most item on the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Tuple1)]
    public static void CreateTuple1(Stack stack) {
        var item1 = stack.Pop();
        stack.Push(new Tuple<object?>(item1));
    }

    /// <summary>
    /// Creates a two value-tuple from the two top most items on the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Tuple2)]
    public static void CreateTuple2(Stack stack) {
        var item1 = stack.Pop();
        var item2 = stack.Pop();
        stack.Push(new Tuple<object?, object?>(item2, item1));
    }

    /// <summary>
    /// Creates a three value-tuple from the three top most items on the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Tuple3)]
    public static void CreateTuple3(Stack stack) {
        var item1 = stack.Pop();
        var item2 = stack.Pop();
        var item3 = stack.Pop();
        stack.Push(new Tuple<object?, object?, object?>(item3, item2, item1));
    }

    /// <summary>
    /// Pushes <see langword="true"/> on to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.NewTrue)]
    public static void PushTrue(Stack stack) => stack.Push(true);

    /// <summary>
    /// Pushes <see langword="false"/> on to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.NewFalse)]
    public static void PushFalse(Stack stack) => stack.Push(false);

    /// <summary>
    /// Pushes a long on to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream"/> to read the data from.</param>
    [PickleMethod(PickleOpCodes.Long1)]
    public static void ReadLong1(Stack stack, Stream stream) {
        // Read length of long as a single byte value.
        var length = stream.ReadByte();
        switch (length) {
            case -1:
                throw new UnpicklingException("EOF reached.");
            case > sizeof(long):
                throw new UnpicklingException($"Invalid long size (max: {sizeof(long)}, got: {length})");
        }
        // Allocate buffer and clear it before reading.
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        buffer.Clear();
        if(stream.Read(buffer[..length]) != length)
            throw new UnpicklingException($"Buffer length mismatch! (expected {length} bytes)");
        stack.Push(BinaryPrimitives.ReadInt64LittleEndian(buffer));
    }

    /// <summary>
    /// Pushes a long on to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream"/> to read the data from.</param>
    [PickleMethod(PickleOpCodes.Long4)]
    public static void ReadLong4(Stack stack, Stream stream) {
        var length = stream.ReadInt32LittleEndian();
        ReadOnlySpan<byte> buffer = stream.ReadSpan(length);
        var value = long.Parse(Encoding.UTF8.GetString(buffer), NumberStyles.Any, CultureInfo.InvariantCulture);
        stack.Push(value);
    }

    /// <summary>
    /// Creates an instance of a previously pushed python type and pushes the instance to the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.NewObj)]
    public static void NewObj(Stack stack) {
        var arg = stack.Pop();
        Type type = stack.Pop() as Type ?? throw new UnpicklingException("The second element on the stack is not a type!");
        stack.Push(Instantiate(type, arg));
    }
}