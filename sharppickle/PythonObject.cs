namespace sharppickle {
    /// <summary>
    ///     Provides a template to implement a Python object, which can be deserialized using <see cref="PickleReader"/>.
    /// </summary>
    public abstract class PythonObject {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PythonObject"/> class.
        /// </summary>
        protected PythonObject() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PythonObject"/> class.
        /// </summary>
        /// <param name="args">The arguments to create the object with.</param>
        protected PythonObject(params object[] args) { }

        /// <summary>
        ///     Sets the state of the object.
        /// </summary>
        /// <param name="obj">The object state to apply.</param>
        public abstract void SetState(object obj);
    }
}
