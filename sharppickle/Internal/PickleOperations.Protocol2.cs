using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal;

/// <summary>
///     Provides the implementation of all op-codes defined in protocol version 2.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    ///     Creates a single tuple from the top most item on the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Tuple1)]
    public static void CreateTuple1(PickleReaderState state) {
        var item1 = state.Stack.Pop();
        state.Stack.Push(new Tuple<object?>(item1));
    }

    /// <summary>
    ///     Creates a two value-tuple from the two top most items on the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Tuple2)]
    public static void CreateTuple2(PickleReaderState state) {
        var item1 = state.Stack.Pop();
        var item2 = state.Stack.Pop();
        state.Stack.Push(new Tuple<object?, object?>(item2, item1));
    }

    /// <summary>
    ///     Creates a three value-tuple from the three top most items on the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Tuple3)]
    public static void CreateTuple3(PickleReaderState state) {
        var item1 = state.Stack.Pop();
        var item2 = state.Stack.Pop();
        var item3 = state.Stack.Pop();
        state.Stack.Push(new Tuple<object?, object?, object?>(item3, item2, item1));
    }

    /// <summary>
    ///     Pushes <see langword="true" /> on to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.NewTrue)]
    public static void PushTrue(PickleReaderState state) => state.Stack.Push(true);

    /// <summary>
    ///     Pushes <see langword="false" /> on to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.NewFalse)]
    public static void PushFalse(PickleReaderState state) => state.Stack.Push(false);

    /// <summary>
    ///     Pushes a long on to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Long1)]
    public static void ReadLong1(PickleReaderState state) {
        // Read length of long as a single byte value.
        var length = state.Stream.ReadByte();
        switch (length) {
            case -1:
                throw new UnpicklingException("EOF reached.");
            case > sizeof(long):
                throw new UnpicklingException($"Invalid long size (max: {sizeof(long)}, got: {length})");
        }

        // Allocate buffer and clear it before reading.
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        if (state.Stream.Read(buffer[..length]) != length)
            throw new UnpicklingException($"Buffer length mismatch! (expected {length} bytes)");
        state.Stack.Push(BinaryPrimitives.ReadInt64LittleEndian(buffer));
    }

    /// <summary>
    ///     Pushes a long on to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Long4)]
    public static void ReadLong4(PickleReaderState state) {
        var length = state.Stream.ReadInt32LittleEndian();
        ReadOnlySpan<byte> buffer = state.Stream.ReadSpan(length);
        var value = long.Parse(Encoding.UTF8.GetString(buffer), NumberStyles.Any, CultureInfo.InvariantCulture);
        state.Stack.Push(value);
    }

    /// <summary>
    ///     Creates an instance of a previously pushed python type and pushes the instance to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.NewObj)]
    public static void NewObj(PickleReaderState state) {
        var args = state.Stack.Pop();
        Type type = state.Stack.Pop<Type>();
        state.Stack.Push(Instantiate(type, args?.GetType().IsArray == true ? args as object[] : [args]));
    }
}
