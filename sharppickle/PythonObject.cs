using System;
using System.Collections.Generic;
using System.Text;

namespace sharppickle {
    /// <summary>
    ///     Provides a base class to implement a python object, which can be unpickled using <see cref="PickleReader"/>.
    /// </summary>
    public abstract class PythonObject {
        protected PythonObject() { }

        protected PythonObject(params object[] args) { }

        public abstract void SetState(object obj);
    }
}
