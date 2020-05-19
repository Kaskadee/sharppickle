using System.IO;
using System.Text;

namespace sharppickle.Extensions {
    /// <summary>
    ///     Provides extension methods to simplify retrieving data from a <seealso cref="Stream"/>.
    /// </summary>
    internal static class StreamExtensions {
        /// <summary>
        ///     Reads a string from the specified stream until a new line character (\n) has been read.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read the string from.</param>
        /// <returns>The string read from the specified <see cref="Stream"/>.</returns>
        public static string ReadLine(this Stream stream) {
            var sb = new StringBuilder();
            while (stream.Position < stream.Length) {
                var b = (char)stream.ReadByte();
                if (b == '\n')
                    break;
                sb.Append(b);
            }

            return sb.ToString();
        }
    }
}
