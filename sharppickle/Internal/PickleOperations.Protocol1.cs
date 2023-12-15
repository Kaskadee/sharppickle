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
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Mark)]
    public static void SetMark(PickleReaderState state) => state.Stack.Push(new Mark());

    /// <summary>
    ///     Discards the top most item on the specified stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Pop)]
    public static void Pop(PickleReaderState state) => state.Stack.Pop();

    /// <summary>
    ///     Discards the stack through the top most <see cref="Mark" /> object.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.PopMark)]
    public static void PopMark(PickleReaderState state) => PopMarkInternal(state);

    /// <summary>
    ///     Discards the stack through the top most <see cref="Mark" /> object.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    /// <returns>An list with the discarded items.</returns>
    private static List<object?> PopMarkInternal(PickleReaderState state) {
        var list = new List<object?>();
        while (state.Stack.Count > 0) {
            if (state.Stack.Peek() is Mark) {
                state.Stack.Pop();
                break;
            }

            list.Add(state.Stack.Pop());
        }

        list.Reverse();
        return list;
    }

    /// <summary>
    ///     Duplicates the top most item on the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Dup)]
    public static void Duplicate(PickleReaderState state) => state.Stack.Push(state.Stack.Peek());

    #endregion

    #region Values

    /// <summary>
    ///     Pushes a <see langword="float" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Float)]
    public static void PushFloat(PickleReaderState state) {
        var s = state.Stream.ReadLine();
        state.Stack.Push(float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Pushes a <see langword="float" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinFloat)]
    public static void PushBinaryFloat(PickleReaderState state) {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        var bytesRead = state.Stream.Read(buffer);
        if (bytesRead != buffer.Length)
            throw new UnpicklingException($"Buffer length mismatch! (read: {bytesRead}, required: {buffer.Length})");
        var value = BinaryPrimitives.ReadDoubleBigEndian(buffer);
        state.Stack.Push(value);
    }

    /// <summary>
    ///     Pushes a <see langword="long" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Long)]
    public static void PushLong(PickleReaderState state) {
        var s = state.Stream.ReadLine();
        if (s.EndsWith("L", StringComparison.OrdinalIgnoreCase))
            s = s[..^1];
        state.Stack.Push(long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Pushes a <see langword="int" /> or a <see langword="bool" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Int)]
    public static void PushInteger(PickleReaderState state) {
        var s = state.Stream.ReadLine();
        switch (s) {
            case "01":
                state.Stack.Push(true);
                return;
            case "00":
                state.Stack.Push(false);
                return;
            default:
                state.Stack.Push(int.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <summary>
    ///     Pushes a <see langword="int" /> with the length of four bytes.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinInt)]
    public static void PushBinaryInt32(PickleReaderState state) => state.Stack.Push(state.Stream.ReadInt32LittleEndian());

    /// <summary>
    ///     Pushes a <see langword="uint" /> with the length of one byte.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinInt1)]
    public static void PushBinaryUInt8(PickleReaderState state) => state.Stack.Push(state.Stream.ReadByte());

    /// <summary>
    ///     Pushes a <see langword="uint" /> with the length of two bytes.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinInt2)]
    public static void PushBinaryUInt16(PickleReaderState state) => state.Stack.Push(state.Stream.ReadUInt16LittleEndian());

    /// <summary>
    ///     Pushes a <see langword="string" /> terminated by a newline character on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.String)]
    public static void PushString(PickleReaderState state) {
        var str = state.Stream.ReadLine();
        if (str.Length < 2 || str[0] != str.Last() || str[0] != '\'')
            throw new UnpicklingException("The STRING op-code argument must be quoted");
        state.Stack.Push(str[1..^1]);
    }

    /// <summary>
    ///     Pushes a length-prefixed <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinString)]
    public static void PushBinaryString(PickleReaderState state) {
        var length = state.Stream.ReadInt32LittleEndian();
        if (length < 0)
            throw new UnpicklingException("BINSTRING pickle has negative byte count");
        ReadOnlyMemory<byte> data = state.Stream.ReadMemory(length);
        if (state.Encoding == null) {
            state.Stack.Push(data);
            return;
        }

        state.Stack.Push(state.Encoding.GetString(data.Span));
    }

    /// <summary>
    ///     Pushes a length-prefixed (1 byte) <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.ShortBinString)]
    public static void PushShortBinaryString(PickleReaderState state) {
        var length = state.Stream.ReadByte();
        ReadOnlyMemory<byte> data = state.Stream.ReadMemory(length);
        if (state.Encoding == null) {
            state.Stack.Push(data);
            return;
        }

        state.Stack.Push(state.Encoding.GetString(data.Span));
    }

    /// <summary>
    ///     Pushes a length-prefixed unicode-escaped <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Unicode)]
    public static void PushUnicode(PickleReaderState state) => state.Stack.Push(state.Stream.ReadUnicodeString());

    /// <summary>
    ///     Pushes a length-prefixed (1 byte) unicode-escaped <see langword="string" /> on to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinUnicode)]
    public static void PushBinaryUnicode(PickleReaderState state) {
        var length = state.Stream.ReadInt32LittleEndian();
        ReadOnlySpan<byte> buffer = state.Stream.ReadSpan(length);
        state.Stack.Push(Encoding.UTF8.GetString(buffer));
    }

    /// <summary>
    ///     Pushes <see langword="null" /> to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.None)]
    public static void PushNone(PickleReaderState state) => state.Stack.Push(null);

    #endregion

    #region Memory

    /// <summary>
    ///     Pushes an item from the memory on the stack. The index is read as a string and parsed to an integer.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Get)]
    public static void Get(PickleReaderState state) {
        var index = int.Parse(state.Stream.ReadLine());
        state.Stack.Push(state.Memo[index]);
    }

    /// <summary>
    ///     Pushes an item from the memory on the stack. The index is read as a byte.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinGet)]
    public static void BinaryGet(PickleReaderState state) {
        var index = state.Stream.ReadByte();
        state.Stack.Push(state.Memo[index]);
    }

    /// <summary>
    ///     Pushes an item from the memory on the stack. The index is read as a signed integer.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.LongBinGet)]
    public static void LongBinaryGet(PickleReaderState state) {
        var i = state.Stream.ReadUInt32LittleEndian();
        if (i > int.MaxValue)
            throw new UnpicklingException("Negative GET argument.");
        state.Stack.Push(state.Memo[(int)i]);
    }

    /// <summary>
    ///     Pops the top most item from the stack and adds it to the memory. The index is read as a string and parsed to an
    ///     integer.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Put)]
    public static void Put(PickleReaderState state) {
        var i = int.Parse(state.Stream.ReadLine());
        if (i < 0)
            throw new UnpicklingException("Negative PUT argument.");
        state.Memo[i] = state.Stack.Peek();
    }

    /// <summary>
    ///     Pops the top most item from the stack and adds it to the memory. The index is read as a byte.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.BinPut)]
    public static void BinaryPut(PickleReaderState state) {
        var i = state.Stream.ReadByte();
        if (i < 0)
            throw new UnpicklingException("Negative PUT argument.");
        state.Memo[i] = state.Stack.Peek();
    }

    /// <summary>
    ///     Pops the top most item from the stack and adds it to the memory. The index is read as a signed integer.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.LongBinPut)]
    public static void LongBinaryPut(PickleReaderState state) {
        var i = state.Stream.ReadUInt32LittleEndian();
        if (i > int.MaxValue)
            throw new UnpicklingException("Negative PUT argument.");
        state.Memo[(int)i] = state.Stack.Peek();
    }

    #endregion

    #region Collections

    /// <summary>
    ///     Pushes an <see cref="IDictionary" /> with the top most items to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Dict)]
    public static void CreateDictionary(PickleReaderState state) {
        List<object?> items = PopMarkInternal(state);
        Dictionary<object, object?> dict = new();
        for (var i = 0; i < items.Count; i += 2) {
            var key = items[i] ?? throw new UnpicklingException("The key cannot be null.");
            dict.Add(key, items[i + 1]);
        }

        state.Stack.Push(dict);
    }

    /// <summary>
    ///     Pushes an empty <see cref="IDictionary" /> to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.EmptyDict)]
    public static void CreateEmptyDictionary(PickleReaderState state) => state.Stack.Push(new Dictionary<object, object?>());

    /// <summary>
    ///     Pushes an <see cref="IList" /> with the top most items to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.List)]
    public static void CreateList(PickleReaderState state) => state.Stack.Push(PopMarkInternal(state));

    /// <summary>
    ///     Pushes an empty <see cref="IList" /> to the top of the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.EmptyList)]
    public static void CreateEmptyList(PickleReaderState state) => state.Stack.Push(new List<object?>());

    /// <summary>
    ///     Pushes an <see cref="Array" /> wih the top most items to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Tuple)]
    public static void CreateTuple(PickleReaderState state) {
        List<object?> items = PopMarkInternal(state);
        var arr = new object[items.Count];
        items.CopyTo(arr, 0);
        state.Stack.Push(arr);
    }

    /// <summary>
    ///     Pushes an empty <see cref="Array" /> to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.EmptyTuple)]
    public static void CreateEmptyTuple(PickleReaderState state) => state.Stack.Push(Array.Empty<object>());

    #endregion

    #region Collection Manipulation

    /// <summary>
    ///     Pops the top most item from the <see cref="Stack" /> and adds it to the list below it.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Append)]
    public static void Append(PickleReaderState state) {
        var value = state.Stack.Pop();
        IList list = state.Stack.Peek<IList>();
        list.Add(value);
    }

    /// <summary>
    ///     Pops the top most items from the <see cref="Stack" /> and adds them to the list below it.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Appends)]
    public static void Appends(PickleReaderState state) {
        List<object?> items = PopMarkInternal(state);
        IList list = state.Stack.Peek<IList>();
        foreach (var i in items)
            list.Add(i);
    }

    /// <summary>
    ///     Pops a value and a key from the specified <see cref="Stack" /> and pushes them to the dictionary below them.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.SetItem)]
    public static void SetItem(PickleReaderState state) {
        var value = state.Stack.Pop();
        var key = state.Stack.Pop() ?? throw new UnpicklingException("The key cannot be null!");
        IDictionary<object, object?> dict = state.Stack.Peek<IDictionary<object, object?>>();
        dict[key] = value;
    }

    /// <summary>
    ///     Pops values and keys until the next <see cref="Mark" /> object and pushes them to the dictionary below them.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.SetItems)]
    public static void SetItems(PickleReaderState state) {
        List<object?> items = PopMarkInternal(state);
        IDictionary<object, object?> dict = state.Stack.Peek<IDictionary<object, object?>>();
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
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Build)]
    public static void Build(PickleReaderState state) {
        var data = state.Stack.Pop();
        PythonObject inst = state.Stack.Peek<PythonObject>();
        inst.SetState(data);
    }

    /// <summary>
    ///     Pushes a python type (mapped by a registered proxy object) to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Global)]
    public static void Global(PickleReaderState state) {
        try {
            state.Stack.Push(state.Reader.GetProxyObject(state.Stream.ReadLine(), state.Stream.ReadLine()));
        } catch (Exception ex) {
            throw new UnpicklingException("An exception occured trying to proccess the GLOBAL op-code!", ex);
        }
    }

    /// <summary>
    ///     Creates an instance of a previously pushed python type and pushes the instance to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Obj)]
    public static void Obj(PickleReaderState state) {
        var args = PopMarkInternal(state).Cast<object>().ToArray();
        if (args.FirstOrDefault() is not Type)
            throw new UnpicklingException("The first element on the stack is not a type or null!");
        state.Stack.Push(Instantiate((args.First() as Type)!, args.Skip(1).ToArray()));
    }

    /// <summary>
    ///     Creates a new instance of a python type (mapped by a registered proxy object) and pushes the instance to the stack.
    /// </summary>
    /// <param name="state">The current state of the <see cref="PickleReader"/> as a <see cref="PickleReaderState"/>.</param>
    [PickleMethod(PickleOpCodes.Inst)]
    public static void Inst(PickleReaderState state) {
        try {
            Type type = state.Reader.GetProxyObject(state.Stream.ReadLine(), state.Stream.ReadLine());
            var args = PopMarkInternal(state).Cast<object>().ToArray();
            state.Stack.Push(Instantiate(type, args));
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
