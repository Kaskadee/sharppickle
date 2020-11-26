using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Internal;

namespace sharppickle {
    /// <summary>
    ///     Provides a managed deserializer for data serialized using Python's Pickle format.
    /// </summary>
    public sealed class PickleReader : IDisposable, IAsyncDisposable {
        /// <summary>
        ///     The highest protocol version that can be read by <see cref="PickleReader"/>.
        /// </summary>
        public const int MaximumProtocolVersion = 5;

        /// <summary>
        ///     Gets or sets the encoding used to encode strings read by <see cref="PickleOpCodes.String"/>, <see cref="PickleOpCodes.BinString"/> or <see cref="PickleOpCodes.ShortBinString"/>.
        ///     <para>The default value is ISO-8859-1 (Latin-1), to make sure that strings and byte arrays can be en-/decoded correctly equally. Change it at your own risk!</para>
        /// </summary>
        /// <remarks>If the value is set to <see langword="null"/>, the raw byte array will be pushed to the stack.</remarks>
        public Encoding Encoding { get; set; } = Encoding.GetEncoding("ISO-8859-1");

        private readonly IDictionary<string, IDictionary<string, Type>> _pythonProxyMappings = new Dictionary<string, IDictionary<string, Type>>();

        private readonly Stream _stream;
        private readonly Stream? _outOfBandStream;
        private readonly bool _leaveOpen;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleReader"/> class using the specified serialized data.
        /// </summary>
        /// <param name="data">The serialized data as a byte array.</param>
        /// <param name="outOfBandStream">The stream to read out-of-band data from (can be null).</param>
        public PickleReader(byte[] data, Stream? outOfBandStream = null) : this(new MemoryStream(data), outOfBandStream: outOfBandStream) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleReader"/> class using the specified file.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> for the file to load and read the serialized data from.</param>
        /// <param name="outOfBandStream">The stream to read out-of-band data from (can be null).</param>
        public PickleReader(FileInfo file, Stream? outOfBandStream = null) : this(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read), outOfBandStream: outOfBandStream) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleReader"/> class using the specified <seealso cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <seealso cref="Stream"/> to read the serialized data from.</param>
        /// <param name="leaveOpen">Whether to keep the underlying stream open, after the <see cref="PickleReader"/> instance is disposed.</param>
        /// <param name="outOfBandStream">The stream to read out-of-band data from (can be null).</param>
        public PickleReader(Stream stream, bool leaveOpen = false, Stream? outOfBandStream = null) {
            if(stream == null)
                throw new ArgumentNullException(nameof(stream));
            if(!stream.CanRead || !stream.CanSeek)
                throw new NotSupportedException("The specified stream must be readable and seekable!");
            if(_outOfBandStream?.CanRead == false)
                throw new NotSupportedException("The out-of-band stream must be readable!");
            _stream = stream;
            _leaveOpen = leaveOpen;
            _outOfBandStream = outOfBandStream;
        }

        /// <summary>
        ///     Deserializes the previous specified data and returns the deserialized data objects.
        /// </summary>
        /// <returns>The deserialized objects as an array of objects.</returns>
        public object?[] Unpickle() {
            // Check if pickle version is supported.
            var version = _stream.ReadByte() != (byte) PickleOpCodes.Proto ? 1 : PickleOperations.GetProtocolVersion(_stream);
            if(version > MaximumProtocolVersion)
                throw new NotSupportedException($"The specified pickle is currently not supported. (version: {version})");
            // Open stream in binary reader.
            using var br = new BinaryReader(_stream, Encoding.UTF8);
            var stack = new Stack();
            var memo = new Dictionary<int, object?>();
            // Read file until either STOP signal has been found or stream has reached EOF.
            while (_stream.Position != _stream.Length) {
                var opByte = br.ReadByte();
                if (!Enum.IsDefined(typeof(PickleOpCodes), opByte))
                    throw new InvalidDataException($"Unknown op-code has been read: 0x{opByte:X}");
                var opCode = (PickleOpCodes)opByte;
                // Check if STOP signal has been reached.
                if (opCode == PickleOpCodes.Stop)
                    return stack.ToArray();
                // Find op-code implementation using reflection and the custom pickle method attribute.
                var method = typeof(PickleOperations).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(x => x.GetCustomAttribute<PickleMethodAttribute>()?.OpCode == opCode);
                if (method == null)
                    throw new UnpicklingException($"No implementation for op-code '{opCode}' found!");
                var arguments = new object[method.GetParameters().Length];
                for (var i = 0; i < arguments.Length; i++) {
                    var type = method.GetParameters()[i].ParameterType;
                    if (type == typeof(Stack)) {
                        arguments[i] = stack;
                    } else if(type == typeof(BinaryReader)) {
                        arguments[i] = br;
                    } else if (type == typeof(Stream)) {
                        arguments[i] = br.BaseStream;
                    } else if (type == typeof(Dictionary<int, object?>)) {
                        arguments[i] = memo;
                    } else if (type == typeof(Encoding)) {
                        arguments[i] = Encoding;
                    } else if (type == typeof(PickleReader)) {
                        arguments[i] = this;
                    } else {
                        throw new UnpicklingException($"Unknown argument for op-code implementation '{opCode}': {type}");
                    }
                }
                method.Invoke(null, arguments);
            }

            throw new UnpicklingException("EOF reached without STOP signal.");
        }

        /// <summary>
        ///     Registers a new python proxy object, which can be used to construct objects with the <see cref="PickleOpCodes.Global"/> op-code.
        /// </summary>
        /// <typeparam name="T">The type of the python object to register.</typeparam>
        /// <param name="moduleName">The name of the module under which to register the object.</param>
        /// <param name="name">The name mapping of the object.</param>
        [PublicAPI]
        public void RegisterObject<T>(string moduleName, string name) where T : PythonObject, new() {
            if(_pythonProxyMappings.ContainsKey(moduleName) && _pythonProxyMappings[moduleName].ContainsKey(name))
                throw new ArgumentException("A proxy object with the specified name already exists.", nameof(name));
            if(!_pythonProxyMappings.ContainsKey(moduleName))
                _pythonProxyMappings[moduleName] = new Dictionary<string, Type>();
            _pythonProxyMappings[moduleName][name] = typeof(T);
        }

        /// <summary>
        ///     Gets the proxy type for the specified module and type name.
        /// </summary>
        /// <param name="moduleName">The name of the module.</param>
        /// <param name="name">The name of the type that is being proxied.</param>
        /// <returns>The registered proxy type for the python type.</returns>
        internal Type GetProxyObject(string moduleName, string name) {
            if(!_pythonProxyMappings.ContainsKey(moduleName))
                throw new ArgumentException($"No module with the specified name exists: {moduleName}", nameof(moduleName));
            if(!_pythonProxyMappings[moduleName].ContainsKey(name))
                throw new ArgumentException($"No object with the specified name in the module {moduleName} exists: {name}", nameof(name));
            return _pythonProxyMappings[moduleName][name];
        }

        /// <summary>
        ///     Gets the out-of-band stream if defined.
        /// </summary>
        /// <returns>The out-of-band stream or <c>null</c> if not applicable.</returns>
        internal Stream? GetOutOfBandStream() => _outOfBandStream;

            /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            if(!_leaveOpen)
                _stream.Dispose();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public async ValueTask DisposeAsync() {
            if (!_leaveOpen)
                await _stream.DisposeAsync();
        }
    }
}
