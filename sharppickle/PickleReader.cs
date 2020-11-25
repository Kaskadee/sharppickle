using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using sharppickle.Exceptions;
using sharppickle.Extensions;
using sharppickle.Internal;

namespace sharppickle {
    /// <summary>
    ///     Provides a managed deserializer for data serialized using Python's Pickle format.
    /// </summary>
    public sealed class PickleReader : IDisposable {
        /// <summary>
        ///     The highest protocol version that can be read by <see cref="PickleReader"/>.
        /// </summary>
        public const int MaximumProtocolVersion = 3;

        /// <summary>
        ///     Gets the array of unsupported pickle op-codes, which can't be implemented because of python-specific implementations.
        /// </summary>
        public PickleOpCodes[] UnsupportedOpCodes => new[] {PickleOpCodes.Reduce, PickleOpCodes.PersId, PickleOpCodes.BinPersId, PickleOpCodes.Ext1, PickleOpCodes.Ext2, PickleOpCodes.Ext4};

        /// <summary>
        ///     Gets or sets the encoding used to encode strings read by <see cref="PickleOpCodes.String"/>, <see cref="PickleOpCodes.BinString"/> or <see cref="PickleOpCodes.ShortBinString"/>.
        ///     <para>The default value is ISO-8859-1 (Latin-1), to make sure that strings and byte arrays can be en-/decoded correctly equally. Change it at your own risk!</para>
        /// </summary>
        /// <remarks>If the value is set to <see langword="null"/>, the raw byte array will be pushed to the stack.</remarks>
        public Encoding Encoding { get; set; } = Encoding.GetEncoding("ISO-8859-1");

        private static readonly IDictionary<PickleOpCodes, Action<Stack, BinaryReader, Dictionary<int, object>>> OpCodeMappings = new Dictionary<PickleOpCodes, Action<Stack, BinaryReader, Dictionary<int, object>>> {
            [PickleOpCodes.Mark] = (stack, reader, memo) => PickleOperations.SetMark(stack),
            [PickleOpCodes.Pop] = (stack, reader, memo) => PickleOperations.Pop(stack),
            [PickleOpCodes.PopMark] = (stack, reader, memo) => PickleOperations.PopMark(stack),
            [PickleOpCodes.Dup] = (stack, reader, memo) => PickleOperations.Duplicate(stack),
            [PickleOpCodes.Float] = (stack, reader, memo) => PickleOperations.PushFloat(stack, reader.BaseStream),
            [PickleOpCodes.BinFloat] = (stack, reader, memo) => PickleOperations.PushBinaryFloat(stack, reader),
            [PickleOpCodes.Int] = (stack, reader, memo) => PickleOperations.PushInteger(stack, reader.BaseStream),
            [PickleOpCodes.BinInt] = (stack, reader, memo) => PickleOperations.PushBinaryInt32(stack, reader),
            [PickleOpCodes.BinInt1] = (stack, reader, memo) => PickleOperations.PushBinaryUInt8(stack, reader.BaseStream),
            [PickleOpCodes.BinInt2] = (stack, reader, memo) => PickleOperations.PushBinaryUInt16(stack, reader.BaseStream),
            [PickleOpCodes.Long] = (stack, reader, memo) => PickleOperations.PushLong(stack, reader.BaseStream),
            [PickleOpCodes.None] = (stack, reader, memo) => PickleOperations.PushNone(stack),
            [PickleOpCodes.String] = (stack, reader, memo) => PickleOperations.PushString(stack, reader.BaseStream),
            [PickleOpCodes.Unicode] = (stack, reader, memo) => PickleOperations.PushUnicode(stack, reader),
            [PickleOpCodes.BinUnicode] = (stack, reader, memo) => PickleOperations.PushBinaryUnicode(stack, reader),
            [PickleOpCodes.Append] = (stack, reader, memo) => PickleOperations.Append(stack),
            [PickleOpCodes.Appends] = (stack, reader, memo) => PickleOperations.Appends(stack),
            [PickleOpCodes.Dict] = (stack, reader, memo) => PickleOperations.CreateDictionary(stack),
            [PickleOpCodes.EmptyDict] = (stack, reader, memo) => PickleOperations.CreateEmptyDictionary(stack),
            [PickleOpCodes.List] = (stack, reader, memo) => PickleOperations.CreateList(stack),
            [PickleOpCodes.EmptyList] = (stack, reader, memo) => PickleOperations.CreateEmptyList(stack),
            [PickleOpCodes.Tuple] = (stack, reader, memo) => PickleOperations.CreateTuple(stack),
            [PickleOpCodes.EmptyTuple] = (stack, reader, memo) => PickleOperations.CreateEmptyTuple(stack),
            [PickleOpCodes.BinUnicode] = (stack, reader, memo) => PickleOperations.PushBinaryUnicode(stack, reader),
            [PickleOpCodes.Put] = (stack, reader, memo) => PickleOperations.Put(stack, reader.BaseStream, memo),
            [PickleOpCodes.BinPut] = (stack, reader, memo) => PickleOperations.BinaryPut(stack, reader.BaseStream, memo),
            [PickleOpCodes.LongBinPut] = PickleOperations.LongBinaryPut,
            [PickleOpCodes.Get] = (stack, reader, memo) => PickleOperations.Get(stack, reader.BaseStream, memo),
            [PickleOpCodes.BinGet] = (stack, reader, memo) => PickleOperations.BinaryGet(stack, reader.BaseStream, memo),
            [PickleOpCodes.LongBinGet] = PickleOperations.LongBinaryGet,
            [PickleOpCodes.SetItem] = (stack, reader, memo) => PickleOperations.SetItem(stack),
            [PickleOpCodes.SetItems] = (stack, reader, memo) => PickleOperations.SetItems(stack),
            // Protocol 2.x op-code mappings
            [PickleOpCodes.Proto] = (stack, reader, memo) => PickleOperations.GetProtocolVersion(reader.BaseStream),
            [PickleOpCodes.Tuple1] = (stack, reader, memo) => PickleOperations.CreateTuple1(stack),
            [PickleOpCodes.Tuple2] = (stack, reader, memo) => PickleOperations.CreateTuple2(stack),
            [PickleOpCodes.Tuple3] = (stack, reader, memo) => PickleOperations.CreateTuple3(stack),
            [PickleOpCodes.NewTrue] = (stack, reader, memo) => PickleOperations.PushTrue(stack),
            [PickleOpCodes.NewFalse] = (stack, reader, memo) => PickleOperations.PushFalse(stack),
            [PickleOpCodes.Long1] = (stack, reader, memo) => PickleOperations.ReadLong1(stack, reader),
            [PickleOpCodes.Long4] = (stack, reader, memo) => PickleOperations.ReadLong4(stack, reader),
            // Protocol 3.x op-code mappings
            [PickleOpCodes.BinaryBytes] = (stack, reader, memo) => PickleOperations.PushBytes(stack, reader.BaseStream),
            [PickleOpCodes.ShortBinaryBytes] = (stack, reader, memo) => PickleOperations.PushShortBytes(stack, reader.BaseStream),
        };
        private readonly IDictionary<string, IDictionary<string, Type>> _pythonProxyMappings = new Dictionary<string, IDictionary<string, Type>>();

        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleReader"/> class using the specified file.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> for the file to load and read the serialized data from.</param>
        public PickleReader(FileInfo file) {
            if(file == null)
                throw new ArgumentNullException(nameof(file));
            if(!file.Exists)
                throw new FileNotFoundException("Failed to open specified file.", file.FullName);
            _stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleReader"/> class using the specified <seealso cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <seealso cref="Stream"/> to read the serialized data from.</param>
        /// <param name="leaveOpen">Whether to keep the underlying stream open, after the <see cref="PickleReader"/> instance is disposed.</param>
        public PickleReader(Stream stream, bool leaveOpen = false) {
            if(stream == null)
                throw new ArgumentNullException(nameof(stream));
            if(!stream.CanRead || !stream.CanSeek)
                throw new NotSupportedException("The specified stream must be readable and seekable!");
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleReader"/> class using the specified serialized data.
        /// </summary>
        /// <param name="data">The serialized data as a byte array.</param>
        public PickleReader(byte[] data) {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            if(data.Length == 0)
                throw new ArgumentException("No input data has been specified.", nameof(data));
            _stream = new MemoryStream(data);
        }

        /// <summary>
        ///     Deserializes the previous specified data and returns the deserialized data objects.
        /// </summary>
        /// <returns>The deserialized objects as an array of objects.</returns>
        public object[] Unpickle() {
            // Check if pickle version is supported.
            var version = _stream.ReadByte() != (byte) PickleOpCodes.Proto ? 1 : PickleOperations.GetProtocolVersion(_stream);
            if(version > MaximumProtocolVersion)
                throw new NotSupportedException($"The specified pickle is currently not supported. (version: {version})");
            // Open stream in binary reader.
            using var br = new BinaryReader(_stream, Encoding.UTF8);
            var stack = new Stack();
            var memo = new Dictionary<int, object>();
            // Read file until either STOP signal has been found or stream has reached EOF.
            while (_stream.Position != _stream.Length) {
                // Parse op-code.
                var opByte = br.ReadByte();
                if (!Enum.IsDefined(typeof(PickleOpCodes), opByte))
                    throw new InvalidDataException($"Unknown op-code has been read: 0x{opByte:X}");
                var opCode = (PickleOpCodes)opByte;
                // Check if STOP signal has been reached.
                if (opCode == PickleOpCodes.Stop)
                    return stack.ToArray();
                // Check if op-code is officially unsupported.
                if (UnsupportedOpCodes.Contains(opCode))
                    throw new NotSupportedException($"Unsupported op-code '{opCode}' has been read!");
                // Invoke op-code mappings.
                if (OpCodeMappings.ContainsKey(opCode)) {
                    OpCodeMappings[opCode]?.Invoke(stack, br, memo);
                    continue;
                } else if (opCode == PickleOpCodes.BinString) {
                    PickleOperations.PushBinaryString(stack, br, Encoding);
                    continue;
                } else if (opCode == PickleOpCodes.ShortBinString) {
                    PickleOperations.PushShortBinaryString(stack, br, Encoding);
                    continue;
                }
                // Handle special op-codes.
                switch (opCode) {
                    case PickleOpCodes.Build:
                        var state = stack.Pop();
                        var inst = (PythonObject) stack.Peek();
                        inst.SetState(state);
                        break;
                    case PickleOpCodes.Global:
                        var module = _stream.ReadLine();
                        var name = _stream.ReadLine();
                        if(!_pythonProxyMappings.ContainsKey(module) || !_pythonProxyMappings[module].ContainsKey(name))
                            throw new UnpicklingException($"No proxy object for the type {module}.{name} has been found!");
                        stack.Push(_pythonProxyMappings[module][name]);
                        break;
                    case PickleOpCodes.Obj:
                        var args = PickleOperations.PopMark(stack).Cast<object>().ToArray();
                        var t = (Type) args[0];
                        stack.Push(Instantiate(t, args.Skip(1).ToArray()));
                        break;
                    case PickleOpCodes.NewObj:
                        var newObjArg = stack.Pop();
                        var newT = (Type) stack.Pop();
                        stack.Push(Instantiate(newT, newObjArg));
                        break;
                    case PickleOpCodes.Inst:
                        var moduleInst = _stream.ReadLine();
                        var nameInst = _stream.ReadLine();
                        if (!_pythonProxyMappings.ContainsKey(moduleInst) || !_pythonProxyMappings[moduleInst].ContainsKey(nameInst))
                            throw new UnpicklingException($"No proxy object for the type {moduleInst}.{nameInst} has been found!");
                        var proxyType = _pythonProxyMappings[moduleInst][nameInst];
                        stack.Push(Instantiate(proxyType, PickleOperations.PopMark(stack).Cast<object>().ToArray()));
                        break;
                    default:
                        throw new UnpicklingException($"No mapping for op-code '{opCode}' available!");
                }
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
        ///     Creates an instance of the specified type with the specified arguments.
        /// </summary>
        /// <param name="type">The type of the derived <seealso cref="PythonObject"/>.</param>
        /// <param name="args">The arguments to pass.</param>
        /// <returns>The instantiated <see cref="PythonObject"/>.</returns>
        private PythonObject Instantiate(Type type, params object[] args) {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!type.IsSubclassOf(typeof(PythonObject)))
                throw new ArgumentException($"The specified type must be a subclass of {typeof(PythonObject)}");
            if (args == null || (args is { } argsArray && argsArray.Length == 0))
                return (PythonObject)Activator.CreateInstance(type);
            if (args[0] is object[] arr && arr.Length == 0)
                return (PythonObject)Activator.CreateInstance(type);
            return (PythonObject)Activator.CreateInstance(type, args);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            if(_stream != null && !_leaveOpen)
                _stream.Dispose();
        }
    }
}
