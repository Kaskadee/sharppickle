using System.Runtime.CompilerServices;
using sharppickle.Attributes;
using sharppickle.Exceptions;

namespace sharppickle.Internal;

/// <summary>
///     Provides stub methods for unsupported op-codes.
/// </summary>
internal static partial class PickleOperations {
    [PickleMethod(PickleOpCodes.Reduce)]
    public static void ReduceStub(PickleReaderState state) => ThrowUnsupportedException();

    [PickleMethod(PickleOpCodes.PersId)]
    public static void PersIdStub(PickleReaderState state) => ThrowUnsupportedException();

    [PickleMethod(PickleOpCodes.BinPersId)]
    public static void BinPersIdStub(PickleReaderState state) => ThrowUnsupportedException();

    [PickleMethod(PickleOpCodes.Ext1)]
    public static void Ext1Stub(PickleReaderState state) => ThrowUnsupportedException();

    [PickleMethod(PickleOpCodes.Ext2)]
    public static void Ext2Stub(PickleReaderState state) => ThrowUnsupportedException();

    [PickleMethod(PickleOpCodes.Ext4)]
    public static void Ext4Stub(PickleReaderState state) => ThrowUnsupportedException();
    
    /// <summary>
    ///     Throws an instance of the <see cref="UnpicklingException" /> to indicate that an unsupported op-code has been
    ///     found.
    /// </summary>
    /// <param name="callerMethod">The name of the caller method to parse the unsupported op-code from.</param>
    private static void ThrowUnsupportedException([CallerMemberName] string? callerMethod = null) {
        if (callerMethod is null)
            throw new UnpicklingException("Unsupported op-code found!");
        var value = Enum.Parse(typeof(PickleOpCodes), callerMethod.Replace("Stub", string.Empty, StringComparison.OrdinalIgnoreCase));
        throw new UnpicklingException($"Unsupported op-code '{value}' found!");
    }
}
