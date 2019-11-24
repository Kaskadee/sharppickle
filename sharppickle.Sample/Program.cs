using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace sharppickle.Sample {
    internal sealed class Program {
        static void Main(string[] args) {
            Console.WriteLine($"[Info] Opening file: '{args[0]}'");
            var fi = new FileInfo(args[0]);
            using var fs = fi.OpenRead();
            // Read offset to read from.
            var header = ReadLine(fs);
            var splitted = header.Split((char) 0x20);
            var offset = Convert.ToInt32(splitted[1], 16);
            fs.Seek(offset, SeekOrigin.Begin);
            // Decompress data to get pickle data.
            var sw = Stopwatch.StartNew();
            using var stream = new ZlibStream(fs, CompressionMode.Decompress);
            var data = ReadToEnd(stream);
            sw.Stop();
            Console.WriteLine($"[Info] Decompressing took {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            var indices = RetrieveIndices(data);
            sw.Stop();
            Console.WriteLine($"[Info] Unpickling and casting data took {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"[Info] {indices.Length} indices have been found in the archive!");
            Console.ReadLine();
        }

        public static ArchiveIndex[] RetrieveIndices(byte[] data) {
            // Unpickle serialized data.
            using (var reader = new PickleReader(data)) {
                var deserialized = reader.Unpickle();
                var dict = ((Dictionary<object, object>) deserialized[0]).ToDictionary(key => key.Key as string, value => value.Value as ArrayList);
                return dict.Select(pair => {
                    var (key, value) = pair;
                    var (offset, length, prefix) = value[0] as Tuple<object, object, object>;
                    return new ArchiveIndex(key, Convert.ToInt64(offset), Convert.ToInt32(length), Encoding.UTF8.GetBytes((string) prefix));
                }).ToArray();
            }
        }

        private static string ReadLine(FileStream stream) {
            var sb = new StringBuilder();
            // Search until line-break is found or end of stream has been reached
            while (stream.Position < stream.Length) {
                var newChar = (char)stream.ReadByte();
                if (newChar == '\n')
                    return sb.ToString();
                sb.Append(newChar);
            }

            // Reached end of stream and no line-break has been found.
            return sb.ToString();
        }

        /// <summary>
        ///     Reads all bytes until the end of stream, from the current position of the stream, has been reached.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        private static byte[] ReadToEnd(Stream stream) {
            using (var ms = new MemoryStream()) {
                // Copy contents of stream to memory.
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
