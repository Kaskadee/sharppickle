using System.Buffers.Binary;
using System.Collections;
using System.Globalization;
using System.Text;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal;

/// <summary>
///     Provides the implementation of all op-codes defined in protocol version 1.x.
/// </summary>
internal static partial class PickleOperations {
    #region General

    /// <summary>
    ///     Appends a special <see cref="Mark" /> object to the top of the stack.
    /// </summary>
    /// <param name="stack">The stack to push the mark object to.</param>
    [PickleMethod(PickleOpCodes.Mark)]
    public static void SetMark(Stack stack) => stack.Push(new Mark());

    /// <summary>
    ///     Discards the top most item on the specified stack.
    /// </summary>
    /// <param name="stack">The stack to discard the top most item from.</param>
    [PickleMethod(PickleOpCodes.Pop)]
    public static void Pop(Stack stack) => stack.Pop();

    /// <summary>
    ///     Discards the stack through the top most <see cref="Mark" /> object.
    /// </summary>
    /// <param name="stack">The stack to discard the top from.</param>
    /// <returns>An list with the discarded items.</returns>
    [PickleMethod(PickleOpCodes.PopMark)]
    public static List<object?> PopMark(Stack stack) {
        var list = new List<object?>();
        while (stack.Count > 0) {
            if (stack.Peek() is Mark) {
                stack.Pop();
                break;
            }

            list.Add(stack.Pop());
        }

        list.Reverse();
        return list;
    }

    /// <summary>
    ///     Duplicates the top most item on the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Dup)]
    public static void Duplicate(Stack stack) => stack.Push(stack.Peek());

    #endregion

    #region Values

    /// <summary>
    ///     Pushes a <see langword="float" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the <see langword="float" /> from.</param>
    [PickleMethod(PickleOpCodes.Float)]
    public static void PushFloat(Stack stack, Stream stream) {
        var s = stream.ReadLine();
        stack.Push(float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Pushes a <see langword="float" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinFloat)]
    public static void PushBinaryFloat(Stack stack, Stream stream) {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        var bytesRead = stream.Read(buffer);
        if (bytesRead != buffer.Length)
            throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
        var value = BinaryPrimitives.ReadDoubleBigEndian(buffer);
        stack.Push(value);
    }

    /// <summary>
    ///     Pushes a <see langword="long" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the <see langword="long" /> from.</param>
    [PickleMethod(PickleOpCodes.Long)]
    public static void PushLong(Stack stack, Stream stream) {
        var s = stream.ReadLine();
        if (s.EndsWith("L", StringComparison.OrdinalIgnoreCase))
            s = s[..^1];
        stack.Push(long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Pushes a <see langword="int" /> or a <see langword="bool" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the <see langword="int" /> or <see langword="bool" /> from.</param>
    [PickleMethod(PickleOpCodes.Int)]
    public static void PushInteger(Stack stack, Stream stream) {
        var s = stream.ReadLine();
        switch (s) {
            case "01":
                stack.Push(true);
                return;
            case "00":
                stack.Push(false);
                return;
            default:
                stack.Push(int.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <summary>
    ///     Pushes a <see langword="int" /> with the length of four bytes.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinInt)]
    public static void PushBinaryInt32(Stack stack, Stream stream) => stack.Push(stream.ReadInt32LittleEndian());

    /// <summary>
    ///     Pushes a <see langword="uint" /> with the length of one byte.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinInt1)]
    public static void PushBinaryUInt8(Stack stack, Stream stream) => stack.Push(stream.ReadByte());

    /// <summary>
    ///     Pushes a <see langword="uint" /> with the length of two bytes.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the <see langword="uint" /> from.</param>
    [PickleMethod(PickleOpCodes.BinInt2)]
    public static void PushBinaryUInt16(Stack stack, Stream stream) { stack.Push(stream.ReadUInt16LittleEndian()); }

    /// <summary>
    ///     Pushes a <see langword="string" /> terminated by a newline character on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.String)]
    public static void PushString(Stack stack, Stream stream) {
        var str = stream.ReadLine();
        if (str.Length < 2 || str[0] != str.Last() || str[0] != '\'')
            throw new UnpicklingException("The STRING op-code argument must be quoted");
        stack.Push(str[1..^1]);
    }

    /// <summary>
    ///     Pushes a length-prefixed <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="encoding">The <see cref="Encoding" /> to encode the string with.</param>
    [PickleMethod(PickleOpCodes.BinString)]
    public static void PushBinaryString(Stack stack, Stream stream, Encoding? encoding) {
        var length = stream.ReadInt32LittleEndian();
        if (length < 0)
            throw new UnpicklingException("BINSTRING pickle has negative byte count");
        ReadOnlyMemory<byte> data = stream.ReadMemory(length);
        if (encoding == null) {
            stack.Push(data);
            return;
        }

        stack.Push(encoding.GetString(data.Span));
    }

    /// <summary>
    ///     Pushes a length-prefixed (1 byte) <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="encoding">The <see cref="Encoding" /> to encode the string with.</param>
    [PickleMethod(PickleOpCodes.ShortBinString)]
    public static void PushShortBinaryString(Stack stack, Stream stream, Encoding? encoding) {
        var length = stream.ReadByte();
        ReadOnlyMemory<byte> data = stream.ReadMemory(length);
        if (encoding == null) {
            stack.Push(data);
            return;
        }

        stack.Push(encoding.GetString(data.Span));
    }

    /// <summary>
    ///     Pushes a length-prefixed unicode-escaped <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.Unicode)]
    public static void PushUnicode(Stack stack, Stream stream) => stack.Push(stream.ReadUnicodeString());

    /// <summary>
    ///     Pushes a length-prefixed (1 byte) unicode-escaped <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    [PickleMethod(PickleOpCodes.BinUnicode)]
    public static void PushBinaryUnicode(Stack stack, Stream stream) {
        var length = stream.ReadInt32LittleEndian();
        ReadOnlySpan<byte> buffer = stream.ReadSpan(length);
        stack.Push(Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    ///     Pushes <see langword="null" /> to the top of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.None)]
    public static void PushNone(Stack stack) => stack.Push(null);

    #endregion

    #region Memory

    /// <summary>
    ///     Pushes an item from the memory on the stack. The index is read as a string and parsed to an integer.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="memo">The memory to retrieve the value from.</param>
    [PickleMethod(PickleOpCodes.Get)]
    public static void Get(Stack stack, Stream stream, IDictionary<int, object?> memo) {
        var index = int.Parse(stream.ReadLine());
        stack.Push(memo[index]);
    }

    /// <summary>
    ///     Pushes an item from the memory on the stack. The index is read as a byte.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="memo">The memory to retrieve the value from.</param>
    [PickleMethod(PickleOpCodes.BinGet)]
    public static void BinaryGet(Stack stack, Stream stream, IDictionary<int, object?> memo) {
        var index = stream.ReadByte();
        stack.Push(memo[index]);
    }

    /// <summary>
    ///     Pushes an item from the memory on the stack. The index is read as a signed integer.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the data from.</param>
    /// <param name="memo">The memory to retrieve the value from.</param>
    [PickleMethod(PickleOpCodes.LongBinGet)]
    public static void LongBinaryGet(Stack stack, Stream stream, IDictionary<int, object?> memo) {
        var i = stream.ReadUInt32LittleEndian();
        if (i > int.MaxValue)
            throw new UnpicklingException("Negative GET argument.");
        stack.Push(memo[(int)i]);
    }

    /// <summary>
    ///     Pops the top most item from the stack and adds it to the memory. The index is read as a string and parsed to an
    ///     integer.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The stream to read the index from.</param>
    /// <param name="memo">The memory to retrieve the value from.</param>
    [PickleMethod(PickleOpCodes.Put)]
    public static void Put(Stack stack, Stream stream, IDictionary<int, object?> memo) {
        var i = int.Parse(stream.ReadLine());
        if (i < 0)
            throw new UnpicklingException("Negative PUT argument.");
        memo[i] = stack.Peek();
    }

    /// <summary>
    ///     Pops the top most item from the stack and adds it to the memory. The index is read as a byte.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The stream to read the index from.</param>
    /// <param name="memo">The memory to retrieve the value from.</param>
    [PickleMethod(PickleOpCodes.BinPut)]
    public static void BinaryPut(Stack stack, Stream stream, IDictionary<int, object?> memo) {
        var i = stream.ReadByte();
        if (i < 0)
            throw new UnpicklingException("Negative PUT argument.");
        memo[i] = stack.Peek();
    }

    /// <summary>
    ///     Pops the top most item from the stack and adds it to the memory. The index is read as a signed integer.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the length of the data from.</param>
    /// <param name="memo">The memory to retrieve the value from.</param>
    [PickleMethod(PickleOpCodes.LongBinPut)]
    public static void LongBinaryPut(Stack stack, Stream stream, IDictionary<int, object?> memo) {
        var i = stream.ReadUInt32LittleEndian();
        if (i > int.MaxValue)
            throw new UnpicklingException("Negative PUT argument.");
        memo[(int)i] = stack.Peek();
    }

    #endregion

    #region Collections

    /// <summary>
    ///     Pushes an <see cref="IDictionary" /> with the top most items to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Dict)]
    public static void CreateDictionary(Stack stack) {
        List<object?> items = PopMark(stack);
        Dictionary<object, object?> dict = new();
        for (var i = 0; i < items.Count; i += 2) {
            var key = items[i] ?? throw new UnpicklingException("The key cannot be null.");
            dict.Add(key, items[i + 1]);
        }

        stack.Push(dict);
    }

    /// <summary>
    ///     Pushes an empty <see cref="IDictionary" /> to the top of the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.EmptyDict)]
    public static void CreateEmptyDictionary(Stack stack) => stack.Push(new Dictionary<object, object?>());

    /// <summary>
    ///     Pushes an <see cref="IList" /> with the top most items to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.List)]
    public static void CreateList(Stack stack) => stack.Push(PopMark(stack));

    /// <summary>
    ///     Pushes an empty <see cref="IList" /> to the top of the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.EmptyList)]
    public static void CreateEmptyList(Stack stack) => stack.Push(new List<object?>());

    /// <summary>
    ///     Pushes an <see cref="Array" /> wih the top most items to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Tuple)]
    public static void CreateTuple(Stack stack) {
        List<object?> items = PopMark(stack);
        var arr = new object[items.Count];
        items.CopyTo(arr, 0);
        stack.Push(arr);
    }

    /// <summary>
    ///     Pushes an empty <see cref="Array" /> to the stack.
    /// </summary>
    /// <param name="stack">The stack to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.EmptyTuple)]
    public static void CreateEmptyTuple(Stack stack) => stack.Push(Array.Empty<object>());

    #endregion

    #region Collection Manipulation

    /// <summary>
    ///     Pops the top most item from the <see cref="Stack" /> and adds it to the list below it.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Append)]
    public static void Append(Stack stack) {
        var value = stack.Pop();
        IList list = stack.Peek<IList>();
        list.Add(value);
    }

    /// <summary>
    ///     Pops the top most items from the <see cref="Stack" /> and adds them to the list below it.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Appends)]
    public static void Appends(Stack stack) {
        List<object?> items = PopMark(stack);
        IList list = stack.Peek<IList>();
        foreach (var i in items)
            list.Add(i);
    }

    /// <summary>
    ///     Pops a value and a key from the specified <see cref="Stack" /> and pushes them to the dictionary below them.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.SetItem)]
    public static void SetItem(Stack stack) {
        var value = stack.Pop();
        var key = stack.Pop() ?? throw new UnpicklingException("The key cannot be null!");
        IDictionary<object, object?> dict = stack.Peek<IDictionary<object, object?>>();
        dict[key] = value;
    }

    /// <summary>
    ///     Pops values and keys until the next <see cref="Mark" /> object and pushes them to the dictionary below them.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.SetItems)]
    public static void SetItems(Stack stack) {
        List<object?> items = PopMark(stack);
        IDictionary<object, object?> dict = stack.Peek<IDictionary<object, object?>>();
        for (var i = 0; i < items.Count; i += 2) {
            var key = items[i] ?? throw new UnpicklingException("The key cannot be null!");
            dict[key] = items[i + 1];
        }
    }

    #endregion

    #region Special

    /// <summary>
    ///     Calls <see cref="PythonObject.SetState" /> with arguments from the top elements of the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Build)]
    public static void Build(Stack stack) {
        var state = stack.Pop();
        PythonObject inst = stack.Peek<PythonObject>();
        inst.SetState(state);
    }

    /// <summary>
    ///     Pushes a python type (mapped by a registered proxy object) to the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the module and type name from.</param>
    /// <param name="reader">The <see cref="PickleReader" /> to get the proxy object from.</param>
    [PickleMethod(PickleOpCodes.Global)]
    public static void Global(Stack stack, Stream stream, PickleReader reader) {
        try {
            stack.Push(reader.GetProxyObject(stream.ReadLine(), stream.ReadLine()));
        } catch (Exception ex) {
            throw new UnpicklingException("An exception occured trying to proccess the GLOBAL op-code!", ex);
        }
    }

    /// <summary>
    ///     Creates an instance of a previously pushed python type and pushes the instance to the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    [PickleMethod(PickleOpCodes.Obj)]
    public static void Obj(Stack stack) {
        var args = PopMark(stack).Cast<object>().ToArray();
        if (args.FirstOrDefault() is not Type)
            throw new UnpicklingException("The first element on the stack is not a type or null!");
        stack.Push(Instantiate((args.First() as Type)!, args.Skip(1).ToArray()));
    }

    /// <summary>
    ///     Creates a new instance of a python type (mapped by a registered proxy object) and pushes the instance to the stack.
    /// </summary>
    /// <param name="stack">The <see cref="Stack" /> to perform the operation on.</param>
    /// <param name="stream">The <see cref="Stream" /> to read the module and type name from.</param>
    /// <param name="reader">The <see cref="PickleReader" /> to get the proxy object from.</param>
    [PickleMethod(PickleOpCodes.Inst)]
    public static void Inst(Stack stack, Stream stream, PickleReader reader) {
        try {
            Type type = reader.GetProxyObject(stream.ReadLine(), stream.ReadLine());
            var args = PopMark(stack).Cast<object>().ToArray();
            stack.Push(Instantiate(type, args));
        } catch (Exception ex) {
            throw new UnpicklingException("An exception occured trying to proccess the INST op-code!", ex);
        }
    }

    /// <summary>
    ///     Creates a new instance of the specified subclass of <seealso cref="PythonObject" /> with the specified arguments.
    /// </summary>
    /// <param name="type">The type to instantiate and which is a subclass of <seealso cref="PythonObject" />.</param>
    /// <param name="args">The arguments to pass.</param>
    /// <returns>The instance of the specified type.</returns>
    private static PythonObject Instantiate(Type type, params object?[]? args) {
        if (!type.IsSubclassOf(typeof(PythonObject)))
            throw new ArgumentException($"The specified type must be a subclass of {typeof(PythonObject)}");
        if (args is null || args.Length == 0 || args.First() is object?[] { Length: 0 })
            return (Activator.CreateInstance(type) as PythonObject)!;
        return (Activator.CreateInstance(type, args) as PythonObject)!;
    }

    #endregion
}
