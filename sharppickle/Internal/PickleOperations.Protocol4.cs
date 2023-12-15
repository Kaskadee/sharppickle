using System.Collections.Immutable;
using System.Text;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;
using sharppickle.IO;

namespace sharppickle.Internal;

/// <summary>
///     Provides the implementation of all op-codes defined in protocol version 4.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    ///     Pushes a UTF-8 string to the stack with the length read as a single byte.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.ShortBinaryUnicode)]
    public static void PushShortBinaryUnicode(PickleReaderState state) {
        var length = state.Stream.ReadByte();
        if (length == -1)
            throw new UnpicklingException("EOF reached.");
        Span<byte> buffer = stackalloc byte[length];
        if (state.Stream.Read(buffer) != buffer.Length)
            throw new UnpicklingException($"Buffer length mismatch! (expected {buffer.Length} bytes)");
        state.Stack.Push(Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    ///     Pushes a UTF-8 string to the stack with the length read as a 8-byte long.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinaryUnicode8)]
    public static void PushBinaryUnicode8(PickleReaderState state) {
        var length = state.Stream.ReadInt64LittleEndian();
        ReadOnlySpan<byte> buffer = state.Stream.ReadSpan(length);
        state.Stack.Push(Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    ///     Pushes a byte array to the stack with the length read as a 8-byte long.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinaryBytes8)]
    public static void PushBinaryBytes8(PickleReaderState state) {
        var length = state.Stream.ReadInt64LittleEndian();
        ReadOnlyMemory<byte> data = state.Stream.ReadMemory(length);
        state.Stack.Push(data);
    }

    /// <summary>
    ///     Pushes an empty <see cref="HashSet{T}" /> to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.EmptySet)]
    public static void PushEmptySet(PickleReaderState state) => state.Stack.Push(new HashSet<object>());

    /// <summary>
    ///     Pops the top-most items from the stack and adds them to the top collection.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.AddItems)]
    public static void AddItems(PickleReaderState state) {
        List<object?> elements = PopMarkInternal(state);
        ICollection<object?> set = state.Stack.Peek<ICollection<object?>>();
        foreach (var element in elements)
            set.Add(element);
    }

    /// <summary>
    ///     Pushes an <see cref="ImmutableHashSet{T}" /> to the stack with the elements read from the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.FrozenSet)]
    public static void PushFrozenSet(PickleReaderState state) {
        List<object?> elements = PopMarkInternal(state);
        state.Stack.Push(new HashSet<object?>(elements).ToImmutableHashSet());
    }

    /// <summary>
    ///     Creates an new instance an object with the constructor arguments read from the stack.
    /// </summary>
    /// <remarks>Keyword-only arguments are python specific and not handled in this implementation.</remarks>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.NewObjEx)]
    public static void NewObjEx(PickleReaderState state) {
        _ = state.Stack.Pop(); // keyword-only arguments are a python specific thing.
        NewObj(state);
    }

    /// <summary>
    ///     Pushes the type of a python object (which is mapped to a registered proxy object) with module and type name read
    ///     from the stack as a string.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.StackGlobal)]
    public static void LoadStackGlobal(PickleReaderState state) {
        var name = state.Stack.Pop<string>();
        var module = state.Stack.Pop<string>();
        state.Stack.Push(state.Reader.GetProxyObject(module, name));
    }

    /// <summary>
    ///     Stores the top-most element of the stack in the memo.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Memoize)]
    public static void Memoize(PickleReaderState state) => state.Memo.Add(state.Memo.Count, state.Stack.Peek());

    /// <summary>
    ///     Starts reading the frame from the stream.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    /// <remarks>Frames are used to reduce the number of read calls, improving the performance by loading data chunkwise from the file.</remarks>
    [PickleMethod(PickleOpCodes.Frame)]
    public static void ReadFrame(PickleReaderState state) {
        // Read frame size from the stream.
        var frameSize = state.Stream.ReadInt64LittleEndian();
        (state.Stream as FrameStream)!.ReadFrame(frameSize);
    }
}
