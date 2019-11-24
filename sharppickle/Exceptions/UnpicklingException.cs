using System;

namespace sharppickle.Exceptions {
    /// <summary>
    ///     Provides an exception that is raised when there is a problem unpickling an object, such as a security violation.
    /// </summary>
    /// <remarks>Note that other exceptions may also be raised during unpickling.</remarks>
    public sealed class UnpicklingException : PickleException {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleException"/>.
        /// </summary>
        /// <param name="message">A short description which describes the occured error.</param>
        public UnpicklingException(string message) : base(message) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickleException"/> class which wraps another exception.
        /// </summary>
        /// <param name="message">A short description which describes the occured error.</param>
        /// <param name="innerException">The exception to wrap with this exception object.</param>
        public UnpicklingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
