using System.Collections;
using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using sharppickle.Attributes;
using sharppickle.Exceptions;
using sharppickle.Internal;
using sharppickle.IO;

namespace sharppickle;

/// <summary>
///     Provides a fully-managed deserializer for data serialized using the Python Pickle format.
/// </summary>
[PublicAPI]
public sealed class PickleReader : IDisposable, IAsyncDisposable {
    /// <summary>
    ///     The highest protocol version that can be read by <see cref="PickleReader" />.
    /// </summary>
    public const int MaximumProtocolVersion = 5;

    private readonly FrameStream stream;
    private readonly IEnumerator<Memory<byte>>? buffers;
    private readonly FrozenDictionary<PickleOpCodes, Action<PickleReaderState>> methodMappings;
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
    /// <param name="buffers">The collection of out-of-band buffers (as <see cref="Memory{T}"/> of bytes).</param>
    public PickleReader(byte[] data, IEnumerable<Memory<byte>>? buffers = null) : this(new MemoryStream(data), buffers: buffers) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleReader" /> class using the specified file.
    /// </summary>
    /// <param name="file">The <see cref="FileInfo" /> for the file to load and read the serialized data from.</param>
    /// <param name="buffers">The collection of out-of-band buffers (as <see cref="Memory{T}"/> of bytes).</param>
    public PickleReader(FileInfo file, IEnumerable<Memory<byte>>? buffers = null) : this(file.Open(new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.Read, Options = FileOptions.SequentialScan }), buffers: buffers) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleReader" /> class using the specified <seealso cref="Stream" />.
    /// </summary>
    /// <param name="stream">The <seealso cref="Stream" /> to read the serialized data from.</param>
    /// <param name="leaveOpen">Whether to keep the underlying stream open, after the <see cref="PickleReader" /> instance is disposed.</param>
    /// <param name="buffers">The collection of out-of-band buffers (as <see cref="Memory{T}"/> of bytes).</param>
    public PickleReader(Stream stream, bool leaveOpen = false, IEnumerable<Memory<byte>>? buffers = null) {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
            throw new NotSupportedException("The specified stream must be readable and seekable!");
        this.stream = new(stream, leaveOpen);
        this.buffers = buffers?.GetEnumerator();
        // Load the op-code method implementations for each defined op-code.
        this.methodMappings = this.GetPickleMethodMappings();
    }

    /// <summary>
    ///     Deserializes the previous specified data and returns the deserialized data objects.
    /// </summary>
    /// <returns>The deserialized objects as an array of objects.</returns>
    public object?[] Unpickle() {
        // Check if pickle version is supported.
        var version = this.stream.ReadByte() != (byte)PickleOpCodes.Proto ? 1 : PickleOperations.GetProtocolVersion(this.stream);
        if (version > MaximumProtocolVersion)
            throw new NotSupportedException($"The specified pickle is currently not supported. (version: {version})");
        var stack = new Stack();
        var memo = new Dictionary<int, object?>();
        PickleReaderState state = new(version, this.stream, stack, memo, this.Encoding, this);
        // Read file until either STOP signal has been found or stream has reached EOF.
        while (this.stream.Position != this.stream.Length) {
            var opByte = (byte)this.stream.ReadByte();
            if (!Enum.IsDefined(typeof(PickleOpCodes), opByte))
                throw new InvalidDataException($"Unknown op-code has been read: 0x{opByte:X}");
            var opCode = (PickleOpCodes)opByte;
            // Check if STOP signal has been reached.
            if (opCode == PickleOpCodes.Stop)
                return stack.ToArray();
            // Get and invoke op-code implementation from the mappings dictionary.
            this.methodMappings[opCode].Invoke(state);
        }

        throw new UnpicklingException("EOF reached without STOP signal.");
    }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="PickleReader"/> has been provided with out-of-band buffers.
    /// </summary>
    /// <returns><c>true</c>, if out-of-band buffers are available; otherwise <c>false</c>.</returns>
    internal bool HasBuffers() => this.buffers is not null;

    /// <summary>
    /// Gets the next buffer as a <see cref="Memory{T}"/> of bytes from the specified buffers.
    /// </summary>
    /// <returns>The next buffer from the buffer iterable.</returns>
    /// <exception cref="UnpicklingException">No buffers have been specified.</exception>
    internal Memory<byte> GetNextBuffer() {
        if (this.buffers is null)
            throw new UnpicklingException("No out-of-band buffers have been specified.");
        if (!this.buffers.MoveNext())
            throw new UnpicklingException("Not enough out-of-band buffers provided.");
        Memory<byte> buffer = this.buffers.Current;
        return buffer;
    }

    /// <summary>
    ///     Registers a new python proxy object, which can be used to construct objects with the
    ///     <see cref="PickleOpCodes.Global" /> op-code.
    /// </summary>
    /// <typeparam name="T">The type of the python object to register.</typeparam>
    /// <param name="moduleName">The name of the module under which to register the object.</param>
    /// <param name="name">The name mapping of the object.</param>
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
    /// Gets a dictionary of all <see cref="PickleOpCodes"/> with their corresponding method implementation as a <see cref="MethodInfo"/>.
    /// </summary>
    /// <returns>The dictionary of the op-code method implementation mappings as a <see cref="FrozenDictionary{TKey,TValue}"/>.</returns>
    /// <exception cref="UnpicklingException">No implementation for the op-code {opCode} has been found.</exception>
    private FrozenDictionary<PickleOpCodes, Action<PickleReaderState>> GetPickleMethodMappings() {
        // Get op-code method implementations from the PickleOperations class.
        Type pickleOperationsType = typeof(PickleOperations);
        Type pickleReaderStateType = typeof(PickleReaderState);
        var pickleOperationMethods = pickleOperationsType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                         .Where(x => x.GetCustomAttribute<PickleMethodAttribute>() is not null)
                                                         .Where(x => x.GetParameters().Length == 1 && x.GetParameters().Single().ParameterType == pickleReaderStateType)
                                                         .ToFrozenDictionary(k => k.GetCustomAttribute<PickleMethodAttribute>()!.OpCode, v => (Action<PickleReaderState>)v.CreateDelegate(typeof(Action<PickleReaderState>)));
        // Validate that there is a method implementation for each defined op-code.
        foreach (PickleOpCodes opCode in Enum.GetValues<PickleOpCodes>()) {
            if (opCode is not PickleOpCodes.Stop and not PickleOpCodes.Proto && !pickleOperationMethods.ContainsKey(opCode))
                throw new UnpicklingException($"No implementation for the op-code '{opCode}' has been found!");
        }

        return pickleOperationMethods;
    }
    
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        this.buffers?.Dispose();
        this.stream.Dispose();
    }
    
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public ValueTask DisposeAsync() {
        this.buffers?.Dispose();
        return this.stream.DisposeAsync();
    }
}
