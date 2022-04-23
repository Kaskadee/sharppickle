using JetBrains.Annotations;

namespace sharppickle.Attributes; 

/// <summary>
/// Provides an attribute to indicate that a method implements a pickle op-code.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
internal sealed class PickleMethodAttribute : Attribute {
    /// <summary>
    /// Gets the op-code implemented within the method.
    /// </summary>
    public PickleOpCodes OpCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PickleMethodAttribute"/> class.
    /// </summary>
    /// <param name="opCode">The op-code implemented by this method.</param>
    public PickleMethodAttribute(PickleOpCodes opCode) => this.OpCode = opCode;
}