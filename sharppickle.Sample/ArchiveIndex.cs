using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace sharppickle.Sample {
    /// <summary>
    ///     Represents an RPA archive file index.
    /// </summary>
    internal sealed class ArchiveIndex {
        /// <summary>
        ///     Gets the internal archive path of the file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     Gets the offset of the beginning of the file.
        /// </summary>
        public long Offset { get; }

        /// <summary>
        ///     Gets the length of the file in bytes.
        /// </summary>
        public int Length { get; }

        /// <summary>
        ///     Gets the prefix of this file. This seems to be optional and is appended to the beginning of the file data.
        /// </summary>
        public byte[] Prefix { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArchiveIndex" /> class.
        /// </summary>
        /// <param name="path">The internal file path in the archive.</param>
        /// <param name="offset">The offset of the beginning of the file in the archive.</param>
        /// <param name="length">The length of the file in bytes.</param>
        /// <param name="prefix">The prefix data of this file.</param>
        public ArchiveIndex(string path, long offset, int length, byte[] prefix) {
            FilePath = path;
            Offset = offset;
            Length = length;
            Prefix = prefix;
        }

        public static ArchiveIndex FromEntry(KeyValuePair<string, ArrayList> pair) {
            var (offset, length, prefix) = pair.Value[0] as Tuple<object, object, object>;
            return new ArchiveIndex(pair.Key, Convert.ToInt64(offset), Convert.ToInt32(length), Encoding.UTF8.GetBytes((string)prefix));
        }
    }
}
