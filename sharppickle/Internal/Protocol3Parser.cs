﻿using System.Collections;
using System.IO;

namespace sharppickle.Internal {
    /// <summary>
    ///     Provides a class which is able to handle pickle protocol 3.x op-codes.
    ///     Protocol 3 includes two new op-codes: BINBYTES and SHORT_BINBYTES.
    /// </summary>
    internal static class Protocol3Parser {
        /// <summary>
        ///     Reads the length of the data, then reads the amount of bytes from the stream and pushes the byte array to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the data from.</param>
        public static void PushBytes(Stack stack, Stream stream) {
            // Read little-endian unsigned 32-bit integer.
            var buffer = new byte[sizeof(uint)];
            stream.Read(buffer, 0, buffer.Length);
            var length = 0u;
            for (var i = 0; i < buffer.Length; i++)
                length |= (uint)(buffer[i] << (8 * i));
            // Read number of bytes and push them to the stack.
            buffer = new byte[length];
            stream.Read(buffer, 0, buffer.Length);
            stack.Push(buffer);
        }

        /// <summary>
        ///     Reads the length of the data (as a <see langword="byte"/>), then reads the amount of bytes from the stream and pushes the byte array to the top of the stack.
        /// </summary>
        /// <param name="stack">The <see cref="Stack"/> to perform the operation on.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the data from.</param>
        public static void PushShortBytes(Stack stack, Stream stream) {
            // Read byte as length prefix.
            var length = stream.ReadByte();
            // Read number of bytes and push them to the stack.
            var buffer = new byte[length];
            stream.Read(buffer, 0, buffer.Length);
            stack.Push(buffer);
        }
    }
}