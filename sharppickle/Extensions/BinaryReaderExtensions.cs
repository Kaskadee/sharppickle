using System;
using System.IO;
using System.Text.RegularExpressions;

namespace sharppickle.Extensions {
    /// <summary>
    ///     Provides extension methods to parsing data from a <seealso cref="BinaryReader"/>.
    /// </summary>
    internal static class BinaryReaderExtensions {
        /// <summary>
        ///     Reads a little-endian signed 32-bit integer from the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the integer from.</param>
        /// <returns>The read integer and parsed as a little-endian integer.</returns>
        public static int ReadLittleEndianInt32(this BinaryReader reader) {
            // If platform architecture is little-endian use BinaryReader.ReadInt32() instead.
            if (BitConverter.IsLittleEndian)
                return reader.ReadInt32();
            // Read four bytes from stream and build little-endian integer.
            var bytes = reader.ReadBytes(4);
            return (bytes[3] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[0];
        }

        /// <summary>
        ///     Reads a little-endian signed number with the specified number of bytes from the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the number from.</param>
        /// <param name="n">The number of bytes to read from the stream.</param>
        /// <returns>The parsed little-endian number as a <see cref="long"/>.</returns>
        public static long ReadLittleEndianNumber(this BinaryReader reader, int n) {
            var bytes = reader.ReadBytes(n);
            var result = 0L;
            for (var i = 0; i < bytes.Length; i++)
                result |= ((long) bytes[i]) << (i * 8);
            return result;
        }

        /// <summary>
        ///     Reads an escaped unicode string from the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the integer from.</param>
        /// <returns>The read string with escaped characters.</returns>
        public static string ReadUnicodeString(this BinaryReader reader) {
            var str = reader.BaseStream.ReadLine(false);
            return Regex.Replace(str, @"[^\x00-\x7F]", c => $@"\u{(int) c.Value[0]:x4}");
        }
    }
}
