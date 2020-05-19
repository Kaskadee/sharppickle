using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using sharppickle.Exceptions;
using sharppickle.Extensions;
using sharppickle.Internal;

namespace sharppickle {
    /// <summary>
    ///     Provides a fully managed deserializer for data serialized using Python's Pickle Format.
    /// </summary>
    public sealed class PickleReader : IDisposable {
        /// <summary>
        ///     The highest protocol number we know how to read.
        /// </summary>
        public const int HighestProtocol = 3;

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
            [PickleOpCodes.Mark] = (stack, reader, memo) => Protocol1Parser.SetMark(stack),
            [PickleOpCodes.Pop] = (stack, reader, memo) => Protocol1Parser.Pop(stack),
            [PickleOpCodes.PopMark] = (stack, reader, memo) => Protocol1Parser.PopMark(stack),
            [PickleOpCodes.Dup] = (stack, reader, memo) => Protocol1Parser.Duplicate(stack),
            [PickleOpCodes.Float] = (stack, reader, memo) => Protocol1Parser.PushFloat(stack, reader.BaseStream),
            [PickleOpCodes.BinFloat] = (stack, reader, memo) => Protocol1Parser.PushBinaryFloat(stack, reader),
            [PickleOpCodes.Int] = (stack, reader, memo) => Protocol1Parser.PushInteger(stack, reader.BaseStream),
            [PickleOpCodes.BinInt] = (stack, reader, memo) => Protocol1Parser.PushBinaryInt32(stack, reader),
            [PickleOpCodes.BinInt1] = (stack, reader, memo) => Protocol1Parser.PushBinaryUInt8(stack, reader.BaseStream),
            [PickleOpCodes.BinInt2] = (stack, reader, memo) => Protocol1Parser.PushBinaryUInt16(stack, reader.BaseStream),
            [PickleOpCodes.Long] = (stack, reader, memo) => Protocol1Parser.PushLong(stack, reader.BaseStream),
            [PickleOpCodes.None] = (stack, reader, memo) => Protocol1Parser.PushNone(stack),
            [PickleOpCodes.String] = (stack, reader, memo) => Protocol1Parser.PushString(stack, reader.BaseStream),
            [PickleOpCodes.Unicode] = (stack, reader, memo) => Protocol1Parser.PushUnicode(stack, reader),
            [PickleOpCodes.BinUnicode] = (stack, reader, memo) => Protocol1Parser.PushBinaryUnicode(stack, reader),
            [PickleOpCodes.Append] = (stack, reader, memo) => Protocol1Parser.Append(stack),
            [PickleOpCodes.Appends] = (stack, reader, memo) => Protocol1Parser.Appends(stack),
            [PickleOpCodes.Dict] = (stack, reader, memo) => Protocol1Parser.CreateDictionary(stack),
            [PickleOpCodes.EmptyDict] = (stack, reader, memo) => Protocol1Parser.CreateEmptyDictionary(stack),
            [PickleOpCodes.List] = (stack, reader, memo) => Protocol1Parser.CreateList(stack),
            [PickleOpCodes.EmptyList] = (stack, reader, memo) => Protocol1Parser.CreateEmptyList(stack),
            [PickleOpCodes.Tuple] = (stack, reader, memo) => Protocol1Parser.CreateTuple(stack),
            [PickleOpCodes.EmptyTuple] = (stack, reader, memo) => Protocol1Parser.CreateEmptyTuple(stack),
            [PickleOpCodes.BinUnicode] = (stack, reader, memo) => Protocol1Parser.PushBinaryUnicode(stack, reader),
            [PickleOpCodes.Put] = (stack, reader, memo) => Protocol1Parser.Put(stack, reader.BaseStream, memo),
            [PickleOpCodes.BinPut] = (stack, reader, memo) => Protocol1Parser.BinaryPut(stack, reader.BaseStream, memo),
            [PickleOpCodes.LongBinPut] = Protocol1Parser.LongBinaryPut,
            [PickleOpCodes.Get] = (stack, reader, memo) => Protocol1Parser.Get(stack, reader.BaseStream, memo),
            [PickleOpCodes.BinGet] = (stack, reader, memo) => Protocol1Parser.BinaryGet(stack, reader.BaseStream, memo),
            [PickleOpCodes.LongBinGet] = Protocol1Parser.LongBinaryGet,
            [PickleOpCodes.SetItem] = (stack, reader, memo) => Protocol1Parser.SetItem(stack),
            [PickleOpCodes.SetItems] = (stack, reader, memo) => Protocol1Parser.SetItems(stack),
            // Protocol 2.x op-code mappings
            [PickleOpCodes.Proto] = (stack, reader, memo) => Protocol2Parser.GetProtocolVersion(reader.BaseStream),
            [PickleOpCodes.Tuple1] = (stack, reader, memo) => Protocol2Parser.CreateTuple1(stack),
            [PickleOpCodes.Tuple2] = (stack, reader, memo) => Protocol2Parser.CreateTuple2(stack),
            [PickleOpCodes.Tuple3] = (stack, reader, memo) => Protocol2Parser.CreateTuple3(stack),
            [PickleOpCodes.NewTrue] = (stack, reader, memo) => Protocol2Parser.PushTrue(stack),
            [PickleOpCodes.NewFalse] = (stack, reader, memo) => Protocol2Parser.PushFalse(stack),
            [PickleOpCodes.Long1] = (stack, reader, memo) => Protocol2Parser.ReadLong1(stack, reader),
            [PickleOpCodes.Long4] = (stack, reader, memo) => Protocol2Parser.ReadLong4(stack, reader),
            // Protocol 3.x op-code mappings
            [PickleOpCodes.BinaryBytes] = (stack, reader, memo) => Protocol3Parser.PushBytes(stack, reader.BaseStream),
            [PickleOpCodes.ShortBinaryBytes] = (stack, reader, memo) => Protocol3Parser.PushShortBytes(stack, reader.BaseStream),
        };

        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        private readonly IDictionary<string, IDictionary<string, Type>> _pythonProxyMappings = new Dictionary<string, IDictionary<string, Type>>();

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
        ///     Unpickles the previous specified data and returns the deserialized data objects.
        /// </summary>
        /// <returns>The deserialized objects as an array of objects.</returns>
        public object[] Unpickle() {
            // Check if pickle is supported.
            var version = _stream.ReadByte() != (byte) PickleOpCodes.Proto ? 1 : Protocol2Parser.GetProtocolVersion(_stream);
            if(version > HighestProtocol)
                throw new NotSupportedException($"The specified pickle is currently not supported. (version: {version})");
            using var br = new BinaryReader(_stream, Encoding.UTF8);

            var stack = new Stack();
            var memo = new Dictionary<int, object>();

            while (_stream.Position < _stream.Length) {
                var opByte = br.ReadByte();
                if (!Enum.IsDefined(typeof(PickleOpCodes), opByte))
                    throw new InvalidDataException($"Unknown op-code has been found: 0x{opByte:X}");
                var opCode = (PickleOpCodes)opByte;
                if (opCode == PickleOpCodes.Stop)
                    return stack.ToArray();
                if (UnsupportedOpCodes.Contains(opCode))
                    throw new NotSupportedException($"Unsupported op-code '{opCode}' has been found!");
                // Handle special cases.
                switch (opCode) {
                    case PickleOpCodes.BinString:
                        Protocol1Parser.PushBinaryString(stack, br, Encoding);
                        break;
                    case PickleOpCodes.ShortBinString:
                        Protocol1Parser.PushShortBinaryString(stack, br, Encoding);
                        break;
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
                        var args = Protocol1Parser.PopMark(stack).Cast<object>().ToArray();
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
                        stack.Push(Instantiate(proxyType, Protocol1Parser.PopMark(stack).Cast<object>().ToArray()));
                        break;
                    default:
                        if (!OpCodeMappings.ContainsKey(opCode))
                            throw new UnpicklingException($"No op-code mapping for op-code '{opCode}' found!");
                        OpCodeMappings[opCode]?.Invoke(stack, br, memo);
                        break;
                }
            }

            throw new UnpicklingException("No STOP code has been received.");
        }

        /// <summary>
        ///     Registers a new python proxy object, which can be used to construct objects with the <see cref="PickleOpCodes.Global"/> op-code.
        /// </summary>
        /// <typeparam name="T">The type of the python object to register.</typeparam>
        /// <param name="moduleName">The name of the module under which to register the object.</param>
        /// <param name="name">The name mapping of the object.</param>
        public void RegisterObject<T>(string moduleName, string name) where T : PythonObject, new() {
            if(_pythonProxyMappings.ContainsKey(moduleName) && _pythonProxyMappings[moduleName].ContainsKey(name))
                throw new ArgumentException("A proxy object with the specified name already exists.", nameof(name));
            if(!_pythonProxyMappings.ContainsKey(moduleName))
                _pythonProxyMappings[moduleName] = new Dictionary<string, Type>();
            _pythonProxyMappings[moduleName][name] = typeof(T);
        }

        private PythonObject Instantiate(Type type, params object[] args) {
            if(type == null)
                throw new ArgumentNullException(nameof(type));
            if(!type.IsSubclassOf(typeof(PythonObject)))
                throw new ArgumentException($"The specified type must be a subclass of {typeof(PythonObject)}");
            if (args == null || (args is { } argsArray && argsArray.Length == 0)) 
                return (PythonObject) Activator.CreateInstance(type);
            if(args[0] is object[] arr && arr.Length == 0)
                return (PythonObject)Activator.CreateInstance(type);
            return (PythonObject) Activator.CreateInstance(type, args);
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
