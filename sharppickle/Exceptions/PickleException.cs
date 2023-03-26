namespace sharppickle.Exceptions;

/// <summary>
///     Provides a common base class for the other pickling exceptions.
/// </summary>
public class PickleException : Exception {
    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleException" />.
    /// </summary>
    /// <param name="message">A short description which describes the occured error.</param>
    public PickleException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PickleException" /> class which wraps another exception.
    /// </summary>
    /// <param name="message">A short description which describes the occured error.</param>
    /// <param name="innerException">The exception to wrap with this exception object.</param>
    public PickleException(string message, Exception innerException) : base(message, innerException) { }
}
