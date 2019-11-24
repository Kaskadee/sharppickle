using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace sharppickle.Utilities {
    /// <summary>
    ///     Provides several helper methods to reduce duplicated code and simplify I/O access.
    /// </summary>
    internal static class StreamUtilities {
        /// <summary>
        ///     Reads a string from the specified stream until a new line character (\n) has been read.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read the string from.</param>
        /// <param name="appendNewLine">A value indicating whether the new line character should be appended to the string.</param>
        /// <returns>The string read from the specified <see cref="Stream"/>.</returns>
        public static string ReadLine(this Stream stream, bool appendNewLine = true) {
            var sb = new StringBuilder();
            while (stream.Position < stream.Length) {
                var b = (char)stream.ReadByte();
                if (b == '\n') {
                    if (appendNewLine)
                        sb.Append(b);
                    break;
                }

                sb.Append(b);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Reads a little-endian signed 32-bit integer from the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> to read the integer from.</param>
        /// <returns>The read integer and parsed as a litte-endian integer.</returns>
        public static int ReadLittleEndianInt32(this BinaryReader reader) {
            var bytes = reader.ReadBytes(4);
            return bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
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
