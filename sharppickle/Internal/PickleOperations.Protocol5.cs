using System.Collections;
using System.IO;
using sharppickle.Exceptions;
using sharppickle.Extensions;

namespace sharppickle.Internal {
    /// <summary>
    ///     Provides the implementation of all op-codes defined in protocol version 5.x.
    /// </summary>
    internal static partial class PickleOperations {

        /// <summary>
        ///     Pushes a byte array to the stack with the length read from the stream and the contents read from the out-of-band stream.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the byte array from.</param>
        /// <param name="pickleReader">The <see cref="PickleReader"/> to retrieve the out-of-band stream.</param>
        public static void PushByteArray8(Stack stack, BinaryReader reader, PickleReader pickleReader) {
            var length = reader.ReadLittleEndianNumber(8);
            var buffer = new byte[length];
            var stream = pickleReader.GetOutOfBandStream() ?? throw new UnpicklingException("No out-of-band stream to read from!");
            var readBytes = stream.Read(buffer);
            if(readBytes != buffer.Length)
                throw new UnpicklingException($"Buffer length mismatch! (read: {readBytes}, required: {buffer.Length})");
            stack.Push(buffer);
        }
    }
}
