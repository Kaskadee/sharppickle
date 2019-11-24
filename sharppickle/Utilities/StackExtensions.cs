using System.Collections;
using System.Collections.Generic;
using sharppickle.Exceptions;

namespace sharppickle.Utilities {
    /// <summary>
    ///     Provides several helper methods to provide easier access to the <see cref="Stack{T}"/> class.
    /// </summary>
    internal static class StackExtensions {
        public static T Peek<T>(this Stack stack) {
            var obj = stack.Peek();
            if(!(obj is T value))
                throw new UnpicklingException("The element below the item is not a collection.");
            return value;
        }
    }
}
