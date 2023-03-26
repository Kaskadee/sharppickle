using System.Collections;
using System.Collections.Immutable;
using System.Text;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal;

/// <summary>
///     Provides the implementation of all op-codes defined in protocol version 4.x.
/// </summary>
internal static partial class PickleOperations {
    /// <summary>
    ///     Pushes a UTF-8 string to the stack with the length read as a single byte.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.ShortBinaryUnicode)]
    public static void PushShortBinaryUnicode(Stack stack, Stream stream) {
        var length = stream.ReadByte();
        if (length == -1)
            throw new UnpicklingException("EOF reached.");
        Span<byte> buffer = stackalloc byte[length];
        if (stream.Read(buffer) != buffer.Length)
            throw new UnpicklingException($"Buffer length mismatch! (expected {buffer.Length} bytes)");
        stack.Push(Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    ///     Pushes a UTF-8 string to the stack with the length read as a 8-byte long.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinaryUnicode8)]
    public static void PushBinaryUnicode8(Stack stack, Stream stream) {
        var length = stream.ReadInt64LittleEndian();
        ReadOnlySpan<byte> buffer = stream.ReadSpan(length);
        stack.Push(Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    ///     Pushes a byte array to the stack with the length read as a 8-byte long.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinaryBytes8)]
    public static void PushBinaryBytes8(Stack stack, Stream stream) {
        var length = stream.ReadInt64LittleEndian();
        ReadOnlyMemory<byte> data = stream.ReadMemory(length);
        stack.Push(data);
    }

    /// <summary>
    ///     Pushes an empty <see cref="HashSet{T}" /> to the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.EmptySet)]
    public static void PushEmptySet(Stack stack) => stack.Push(new HashSet<object>());

    /// <summary>
    ///     Pops the top-most items from the stack and adds them to the top collection.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.AddItems)]
    public static void AddItems(Stack stack) {
        List<object?> elements = PopMark(stack);
        ICollection<object?> set = stack.Peek<ICollection<object?>>();
        foreach (var element in elements)
            set.Add(element);
    }

    /// <summary>
    ///     Pushes an <see cref="ImmutableHashSet{T}" /> to the stack with the elements read from the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.FrozenSet)]
    public static void PushFrozenSet(Stack stack) {
        List<object?> elements = PopMark(stack);
        stack.Push(new HashSet<object?>(elements).ToImmutableHashSet());
    }

    /// <summary>
    ///     Creates an new instance an object with the constructor arguments read from the stack.
    /// </summary>
    /// <remarks>Keyword-only arguments are python specific and not handled in this implementation.</remarks>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.NewObjEx)]
    public static void NewObjEx(Stack stack) {
        _ = stack.Pop(); // keyword-only arguments are a python specific thing.
        NewObj(stack);
    }

    /// <summary>
    ///     Pushes the type of a python object (which is mapped to a registered proxy object) with module and type name read
    ///     from the stack as a string.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="reader">The <see cref="PickleReader" /> to retrieve the registered proxy object.</param>
    [PickleMethod(PickleOpCodes.StackGlobal)]
    public static void LoadStackGlobal(Stack stack, PickleReader reader) {
        var name = stack.Pop() as string ?? throw new UnpicklingException("The top-most item on the stack was not a string!");
        var module = stack.Pop() as string ?? throw new UnpicklingException("The top-most item on the stack was not a string!");
        stack.Push(reader.GetProxyObject(module, name));
    }

    /// <summary>
    ///     Stores the top-most element of the stack in the memo.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="memo">The memo dictionary to store the element to.</param>
    [PickleMethod(PickleOpCodes.Memoize)]
    public static void Memoize(Stack stack, IDictionary<int, object?> memo) => memo.Add(memo.Count, stack.Peek());
}
