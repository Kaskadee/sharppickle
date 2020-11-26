using System;
using JetBrains.Annotations;

namespace sharppickle {
    /// <summary>
    ///     Provides a template to implement a Python object, which can be deserialized using <see cref="PickleReader"/>.
    /// </summary>
    public abstract class PythonObject {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PythonObject"/> class.
        /// </summary>
        [PublicAPI]
        protected PythonObject() : this(Array.Empty<object?>()) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PythonObject"/> class.
        /// </summary>
        /// <param name="args">The arguments to create the object with.</param>
        protected PythonObject(params object?[] args) {
            if(args == null)
                throw new ArgumentNullException(nameof(args));
        }

        /// <summary>
        ///     Sets the state of the object.
        /// </summary>
        /// <param name="obj">The object state to apply.</param>
        public abstract void SetState(object? obj);
    }
}
