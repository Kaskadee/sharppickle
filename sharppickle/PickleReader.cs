using System.Collections;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Internal;

namespace sharppickle;

/// <summary>
///     Provides a fully-managed deserializer for data serialized using the Python Pickle format.
/// </summary>
public sealed class PickleReader : IDisposable, IAsyncDisposable {
    /// <summary>
    ///     The highest protocol version that can be read by <see cref="PickleReader" />.
    /// </summary>
    public const int MaximumProtocolVersion = 5;

    private readonly Stream stream;
    private readonly Stream? outOfBandStream;
    private readonly bool leaveOpen;
    private readonly Dictionary<string, IDictionary<string, Type>> pythonProxyMappings = new();
    
    /// <summary>
    ///     Gets or sets the encoding used to encode strings read by <see cref="PickleOpCodes.String" />,
    ///     <see cref="PickleOpCodes.BinString" /> or <see cref="PickleOpCodes.ShortBinString" />.
    ///     <para>
    ///         The default value is ISO-8859-1 (Latin-1), to make sure that strings and byte arrays can be en-/decoded
    ///         correctly equally. Change it at your own risk!
    ///     </para>
    /// </summary>
    /// <remarks>If the value is set to <see langword="null" />, the raw byte array will be pushed to the stack.</remarks>
    public Encoding? Encoding { get; set; } = Encoding.GetEncoding("ISO-8859-1");
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleReader" /> class using the specified serialized data.
    /// </summary>
    /// <param name="data">The serialized data as a byte array.</param>
    /// <param name="outOfBandStream">The stream to read out-of-band data from (can be null).</param>
    public PickleReader(byte[] data, Stream? outOfBandStream = null) : this(new MemoryStream(data), outOfBandStream: outOfBandStream) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleReader" /> class using the specified file.
    /// </summary>
    /// <param name="file">The <see cref="FileInfo" /> for the file to load and read the serialized data from.</param>
    /// <param name="outOfBandStream">The stream to read out-of-band data from (can be null).</param>
    public PickleReader(FileInfo file, Stream? outOfBandStream = null) : this(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read), outOfBandStream: outOfBandStream) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleReader" /> class using the specified <seealso cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <seealso cref="Stream" /> to read the serialized data from.</param>
    /// <param name="leaveOpen">
    ///     Whether to keep the underlying stream open, after the <see cref="PickleReader" /> instance is
    ///     disposed.
    /// </param>
    /// <param name="outOfBandStream">The stream to read out-of-band data from (can be null).</param>
    public PickleReader(Stream stream, bool leaveOpen = false, Stream? outOfBandStream = null) {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead || !stream.CanSeek)
            throw new NotSupportedException("The specified stream must be readable and seekable!");
        if (this.outOfBandStream?.CanRead == false)
            throw new NotSupportedException("The out-of-band stream must be readable!");
        this.stream = stream;
        this.leaveOpen = leaveOpen;
        this.outOfBandStream = outOfBandStream;
    }

    /// <summary>
    ///     Deserializes the previous specified data and returns the deserialized data objects.
    /// </summary>
    /// <returns>The deserialized objects as an array of objects.</returns>
    [PublicAPI]
    public object?[] Unpickle() {
        // Check if pickle version is supported.
        var version = this.stream.ReadByte() != (byte)PickleOpCodes.Proto ? 1 : PickleOperations.GetProtocolVersion(this.stream);
        if (version > MaximumProtocolVersion)
            throw new NotSupportedException($"The specified pickle is currently not supported. (version: {version})");
        var stack = new Stack();
        var memo = new Dictionary<int, object?>();
        // Read file until either STOP signal has been found or stream has reached EOF.
        while (this.stream.Position != this.stream.Length) {
            var opByte = (byte)this.stream.ReadByte();
            if (!Enum.IsDefined(typeof(PickleOpCodes), opByte))
                throw new InvalidDataException($"Unknown op-code has been read: 0x{opByte:X}");
            var opCode = (PickleOpCodes)opByte;
            // Check if STOP signal has been reached.
            if (opCode == PickleOpCodes.Stop)
                return stack.ToArray();
            // Find op-code implementation using reflection and the custom pickle method attribute.
            MethodInfo? method = typeof(PickleOperations).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(x => x.GetCustomAttribute<PickleMethodAttribute>()?.OpCode == opCode);
            if (method == null)
                throw new UnpicklingException($"No implementation for op-code '{opCode}' found!");
            var arguments = new object?[method.GetParameters().Length];
            for (var i = 0; i < arguments.Length; i++) {
                Type type = method.GetParameters()[i].ParameterType;
                if (type == typeof(Stack))
                    arguments[i] = stack;
                else if (type == typeof(Stream))
                    arguments[i] = this.stream;
                else if (type == typeof(Dictionary<int, object?>) || type == typeof(IDictionary<int, object?>))
                    arguments[i] = memo;
                else if (type == typeof(Encoding))
                    arguments[i] = this.Encoding;
                else if (type == typeof(PickleReader))
                    arguments[i] = this;
                else
                    throw new UnpicklingException($"Unknown argument for op-code implementation '{opCode}': {type}");
            }

            method.Invoke(null, arguments);
        }

        throw new UnpicklingException("EOF reached without STOP signal.");
    }

    /// <summary>
    ///     Registers a new python proxy object, which can be used to construct objects with the
    ///     <see cref="PickleOpCodes.Global" /> op-code.
    /// </summary>
    /// <typeparam name="T">The type of the python object to register.</typeparam>
    /// <param name="moduleName">The name of the module under which to register the object.</param>
    /// <param name="name">The name mapping of the object.</param>
    [PublicAPI]
    public void RegisterObject<T>(string moduleName, string name) where T : PythonObject, new() {
        if (this.pythonProxyMappings.ContainsKey(moduleName) && this.pythonProxyMappings[moduleName].ContainsKey(name))
            throw new ArgumentException("A proxy object with the specified name already exists.", nameof(name));
        if (!this.pythonProxyMappings.ContainsKey(moduleName))
            this.pythonProxyMappings[moduleName] = new Dictionary<string, Type>();
        this.pythonProxyMappings[moduleName][name] = typeof(T);
    }

    /// <summary>
    ///     Gets the proxy type for the specified module and type name.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="name">The name of the type that is being proxied.</param>
    /// <returns>The registered proxy type for the python type.</returns>
    internal Type GetProxyObject(string moduleName, string name) {
        if (!this.pythonProxyMappings.ContainsKey(moduleName))
            throw new ArgumentException($"No module with the specified name exists: {moduleName}", nameof(moduleName));
        if (!this.pythonProxyMappings[moduleName].ContainsKey(name))
            throw new ArgumentException($"No object with the specified name in the module {moduleName} exists: {name}", nameof(name));
        return this.pythonProxyMappings[moduleName][name];
    }

    /// <summary>
    ///     Gets the out-of-band stream if defined.
    /// </summary>
    /// <returns>The out-of-band stream or <c>null</c> if not applicable.</returns>
    internal Stream? GetOutOfBandStream() => this.outOfBandStream;
    
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        if (!this.leaveOpen)
            this.stream.Dispose();
    }
    
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public ValueTask DisposeAsync() {
        return !this.leaveOpen ? this.stream.DisposeAsync() : ValueTask.CompletedTask;
    }
}
