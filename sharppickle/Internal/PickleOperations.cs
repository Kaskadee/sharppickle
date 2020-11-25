using System;
using System.IO;

namespace sharppickle.Internal {
    /// <summary>
    ///     Provides a class to provide the C# implementation of pickle op-codes.
    /// </summary>
    internal static partial class PickleOperations {
        /// <summary>
        ///     Provides a special mark object to implement the "MARK" op-code and the associated pickle op-codes.
        /// </summary>
        public sealed class Mark { }

        /// <summary>
        ///     Identifies the pickle protocol and returns the protocol version.
        /// </summary>
        /// <param name="stream">The stream to read the pickle data from.</param>
        /// <returns>The retrieve protocol version of the currently loaded pickle.</returns>
        public static int GetProtocolVersion(Stream stream) {
            if (!stream.CanSeek)
                throw new NotSupportedException("The stream must be seekable!");
            var oldPos = stream.Position;
            try {
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ReadByte();
            } finally {
                stream.Seek(oldPos, SeekOrigin.Begin);
            }
        }
    }
}
