using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal {
    /// <summary>
    ///     Provides a class which is able to handle pickle protocol 1.x op-codes.
    ///     Protocol 1 includes all base op-codes e.g. MARK, STOP, POP, POP_MARK, DUP and so on...
    /// </summary>
    internal static class Protocol1Parser {
        #region General

        /// <summary>
        ///     Appends a special <see cref="Mark"/> object to the top of the stack.
        /// </summary>
        /// <param name="stack">The stack to push the mark object to.</param>
        public static void SetMark(Stack stack) => stack.Push(new Mark());

        /// <summary>
        ///     Discards the top most item on the specified stack.
        /// </summary>
        /// <param name="stack">The stack to discard the top most item from.</param>
        public static void Pop(Stack stack) => stack.Pop();

        /// <summary>
        ///     Discards the stack through the top most <see cref="Mark"/> object.
        /// </summary>
        /// <param name="stack">The stack to discard the top from.</param>
        /// <returns>An list with the discarded items.</returns>
        public static IList PopMark(Stack stack) {
            var list = new ArrayList();
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
        public static void Duplicate(Stack stack) => stack.Push(stack.Peek());

        #endregion

        #region Values

        /// <summary>
        ///     Pushes a <see langword="float"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the <see langword="float"/> from.</param>
        public static void PushFloat(Stack stack, Stream stream) {
            var s = stream.ReadLine();
            stack.Push(float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Pushes a <see langword="float"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the <see langword="float"/> from.</param>
        public static void PushBinaryFloat(Stack stack, BinaryReader reader) {
            var bytes = reader.ReadBytes(8).Reverse().ToArray();
            stack.Push(BitConverter.ToDouble(bytes, 0));
        }

        /// <summary>
        ///     Pushes a <see langword="long"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the <see langword="long"/> from.</param>
        public static void PushLong(Stack stack, Stream stream) {
            var s = stream.ReadLine();
            if (s.EndsWith("L", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - 1);
            stack.Push(long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Pushes a <see langword="int"/> or a <see langword="bool"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the <see langword="int"/> or <see langword="bool"/> from.</param>
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
        ///     Pushes a <see langword="int"/> with the length of four bytes.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the <see langword="int"/> from.</param>
        public static void PushBinaryInt32(Stack stack, BinaryReader reader) => stack.Push(reader.ReadLittleEndianInt32());

        /// <summary>
        ///     Pushes a <see langword="uint"/> with the length of one byte.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the <see langword="uint"/> from.</param>
        public static void PushBinaryUInt8(Stack stack, Stream stream) => stack.Push(stream.ReadByte());

        /// <summary>
        ///     Pushes a <see langword="uint"/> with the length of two bytes.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the <see langword="uint"/> from.</param>
        public static void PushBinaryUInt16(Stack stack, Stream stream) {
            var buffer = new byte[sizeof(ushort)];
            stream.Read(buffer, 0, buffer.Length);
            stack.Push(BitConverter.ToUInt16(buffer, 0));
        }

        /// <summary>
        ///     Pushes a <see langword="string"/> terminated by a newline character on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the <see langword="string"/> from.</param>
        public static void PushString(Stack stack, Stream stream) {
            var str = stream.ReadLine();
            if (str.Length < 2 || str[0] != str.Last() || str[0] != '\'')
                throw new UnpicklingException("The STRING op-code argument must be quoted");
            stack.Push(str.Substring(1, str.Length - 2));
        }

        /// <summary>
        ///     Pushes a length-prefixed <see langword="string"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the <see langword="string"/> from.</param>
        /// <param name="encoding">The <see cref="Encoding"/> to encode the string with.</param>
        public static void PushBinaryString(Stack stack, BinaryReader reader, Encoding encoding) {
            var length = reader.ReadLittleEndianInt32();
            if (length < 0)
                throw new UnpicklingException("BINSTRING pickle has negative byte count");
            var data = reader.ReadBytes(length);
            if (encoding == null) {
                stack.Push(data);
                return;
            }
            stack.Push(encoding.GetString(data));
        }

        /// <summary>
        ///     Pushes a length-prefixed (1 byte) <see langword="string"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the <see langword="string"/> from.</param>
        /// <param name="encoding">The <see cref="Encoding"/> to encode the string with.</param>
        public static void PushShortBinaryString(Stack stack, BinaryReader reader, Encoding encoding) {
            var length = reader.ReadByte();
            var data = reader.ReadBytes(length);
            if (encoding == null) {
                stack.Push(data);
                return;
            }
            stack.Push(encoding.GetString(data));
        }

        /// <summary>
        ///     Pushes a length-prefixed unicode-escaped <see langword="string"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the <see langword="string"/> from.</param>
        public static void PushUnicode(Stack stack, BinaryReader reader) => stack.Push(reader.ReadUnicodeString());

        /// <summary>
        ///     Pushes a length-prefixed (1 byte) unicode-escaped <see langword="string"/> on to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the <see langword="string"/> from.</param>
        public static void PushBinaryUnicode(Stack stack, BinaryReader reader) {
            var length = reader.ReadLittleEndianInt32();
            stack.Push(Encoding.UTF8.GetString(reader.ReadBytes(length)));
        }

        /// <summary>
        ///     Pushes <see langword="null"/> to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        public static void PushNone(Stack stack) => stack.Push(null);

        #endregion

        #region Memory
        /// <summary>
        ///     Pushes an item from the memory on the stack. The index is read as a string and parsed to an integer.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="stream">The stream to read the index from.</param>
        /// <param name="memo">The memory to retrieve the value from.</param>
        public static void Get(Stack stack, Stream stream, IDictionary<int, object> memo) {
            var index = int.Parse(stream.ReadLine());
            stack.Push(memo[index]);
        }

        /// <summary>
        ///     Pushes an item from the memory on the stack. The index is read as a byte.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="stream">The stream to read the index from.</param>
        /// <param name="memo">The memory to retrieve the value from.</param>
        public static void BinaryGet(Stack stack, Stream stream, IDictionary<int, object> memo) {
            var index = stream.ReadByte();
            stack.Push(memo[index]);
        }

        /// <summary>
        ///     Pushes an item from the memory on the stack. The index is read as a signed integer.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="reader">The reader to read the index from.</param>
        /// <param name="memo">The memory to retrieve the value from.</param>
        public static void LongBinaryGet(Stack stack, BinaryReader reader, IDictionary<int, object> memo) {
            var index = reader.ReadLittleEndianInt32();
            stack.Push(memo[index]);
        }

        /// <summary>
        ///     Pops the top most item from the stack and adds it to the memory. The index is read as a string and parsed to an integer.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="stream">The stream to read the index from.</param>
        /// <param name="memo">The memory to retrieve the value from.</param>
        public static void Put(Stack stack, Stream stream, IDictionary<int, object> memo) {
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
        public static void BinaryPut(Stack stack, Stream stream, IDictionary<int, object> memo) {
            var i = stream.ReadByte();
            if (i < 0)
                throw new UnpicklingException("Negative PUT argument.");
            memo[i] = stack.Peek();
        }

        /// <summary>
        ///     Pops the top most item from the stack and adds it to the memory. The index is read as a signed integer.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="reader">The reader to read the index from.</param>
        /// <param name="memo">The memory to retrieve the value from.</param>
        public static void LongBinaryPut(Stack stack, BinaryReader reader, IDictionary<int, object> memo) {
            var i = reader.ReadLittleEndianInt32();
            if (i < 0)
                throw new UnpicklingException("Negative PUT argument.");
            memo[i] = stack.Peek();
        }
        #endregion

        #region Collections

        /// <summary>
        ///     Pushes an <see cref="IDictionary"/> with the top most items to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        public static void CreateDictionary(Stack stack) {
            var items = PopMark(stack);
            var dict = new Dictionary<object, object>();
            for (var i = 0; i < items.Count; i += 2) {
                dict.Add(items[i], items[i + 1]);
            }
            stack.Push(dict);
        }

        /// <summary>
        ///     Pushes an empty <see cref="IDictionary"/> to the top of the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        public static void CreateEmptyDictionary(Stack stack) => stack.Push(new Dictionary<object, object>());

        /// <summary>
        ///     Pushes an <see cref="IList"/> with the top most items to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        public static void CreateList(Stack stack) => stack.Push(PopMark(stack));

        /// <summary>
        ///     Pushes an empty <see cref="IList"/> to the top of the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        public static void CreateEmptyList(Stack stack) => stack.Push(new ArrayList());

        /// <summary>
        ///     Pushes an <see cref="Array"/> wih the top most items to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        public static void CreateTuple(Stack stack) {
            var items = PopMark(stack);
            var arr = new object[items.Count];
            items.CopyTo(arr, 0);
            stack.Push(arr);
        }

        /// <summary>
        ///     Pushes an empty <see cref="Array"/> to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        public static void CreateEmptyTuple(Stack stack) => stack.Push(new object[] { });

        #endregion

        #region Collection Manipulation

        /// <summary>
        ///     Pops the top most item from the <see cref="Stack"/> and adds it to the list below it.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        public static void Append(Stack stack) {
            var value = stack.Pop();
            var list = stack.Peek<IList>();
            list.Add(value);
        }

        /// <summary>
        ///     Pops the top most items from the <see cref="Stack"/> and adds them to the list below it.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        public static void Appends(Stack stack) {
            var items = PopMark(stack);
            var list = stack.Peek<IList>();
            foreach (var i in items)
                list.Add(i);
        }

        /// <summary>
        ///     Pops a value and a key from the specified <see cref="Stack"/> and pushes them to the dictionary below them.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        public static void SetItem(Stack stack) {
            var value = stack.Pop();
            var key = stack.Pop();
            var dict = stack.Peek<IDictionary<object, object>>();
            if(!dict.ContainsKey(key))
                dict.Add(key, value);
            else 
                dict[key] = value;
        }

        /// <summary>
        ///     Pops values and keys until the next <see cref="Mark"/> object and pushes them to the dictionary below them.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        public static void SetItems(Stack stack) {
            var items = PopMark(stack);
            var dict = stack.Peek<IDictionary<object, object>>();
            for (var i = 0; i < items.Count; i += 2) {
                if(!dict.ContainsKey(items[i]))
                    dict.Add(items[i], items[i + 1]);
                else
                    dict[items[i]] = items[i + 1];
            }
        }

        #endregion
    }
}
