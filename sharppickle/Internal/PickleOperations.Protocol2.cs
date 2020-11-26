using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal {
    /// <summary>
    ///     Provides the implementation of all op-codes defined in protocol version 2.x.
    /// </summary>
    internal static partial class PickleOperations {
        /// <summary>
        ///     Creates a single tuple from the top most item on the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        [PickleMethod(PickleOpCodes.Tuple1)]
        public static void CreateTuple1(Stack stack) {
            var item1 = stack.Pop();
            stack.Push(new Tuple<object?>(item1));
        }

        /// <summary>
        ///     Creates a two value-tuple from the two top most items on the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        [PickleMethod(PickleOpCodes.Tuple2)]
        public static void CreateTuple2(Stack stack) {
            var item1 = stack.Pop();
            var item2 = stack.Pop();
            stack.Push(new Tuple<object?, object?>(item2, item1));
        }

        /// <summary>
        ///     Creates a three value-tuple from the three top most items on the stack.
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
        ///     Pushes <see langword="true"/> on to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        [PickleMethod(PickleOpCodes.NewTrue)]
        public static void PushTrue(Stack stack) => stack.Push(true);

        /// <summary>
        ///     Pushes <see langword="false"/> on to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        [PickleMethod(PickleOpCodes.NewFalse)]
        public static void PushFalse(Stack stack) => stack.Push(false);

        /// <summary>
        ///     Pushes a long on to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="reader">The reader to read the length of the long value from.</param>
        [PickleMethod(PickleOpCodes.Long1)]
        public static void ReadLong1(Stack stack, BinaryReader reader) {
            var n = reader.ReadByte();
            stack.Push(reader.ReadLittleEndianNumber(n));
        }

        /// <summary>
        ///     Pushes a long on to the stack.
        /// </summary>
        /// <param name="stack">The stack to perform the operation on.</param>
        /// <param name="reader">The reader to read the length of the long value from.</param>
        [PickleMethod(PickleOpCodes.Long4)]
        public static void ReadLong4(Stack stack, BinaryReader reader) {
            var bytes = reader.ReadBytes(4);
            var n = BitConverter.ToInt32(bytes, 0);
            if(n < 0)
                throw new UnpicklingException("LONG pickle has negative byte count");
            var data = Encoding.UTF8.GetString(reader.ReadBytes(n));
            stack.Push(long.Parse(data, NumberStyles.Any, CultureInfo.InvariantCulture));
        }


        /// <summary>
        ///     Creates an instance of a previously pushed python type and pushes the instance to the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        [PickleMethod(PickleOpCodes.NewObj)]
        public static void NewObj(Stack stack) {
            var arg = stack.Pop();
            var type = stack.Pop() as Type ?? throw new UnpicklingException("The second element on the stack is not a type!");
            stack.Push(Instantiate(type, arg));
        }
    }
}
