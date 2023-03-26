using System.Collections;
using sharppickle.Exceptions;

namespace sharppickle.Extensions;

/// <summary>
///     Provides extension methods to provide additional features for the <see cref="Stack{T}" /> class.
/// </summary>
internal static class StackExtensions {
    /// <summary>
    ///     Returns the first element of the <see cref="Stack" /> without removing it from the top.
    /// </summary>
    /// <typeparam name="T">The expected type of the object.</typeparam>
    /// <param name="stack">The stack to peek the first element from.</param>
    /// <returns>The top object of the <paramref name="stack" /> as the specified type.</returns>
    /// <exception cref="UnpicklingException">The element is of type {type} but {T} was expected.</exception>
    public static T Peek<T>(this Stack stack) {
        var obj = stack.Peek();
        return obj switch {
            null => throw new UnpicklingException("The element is null."),
            T value => value,
            var _ => throw new UnpicklingException($"The element is of type {obj.GetType().Name} but {typeof(T).Name} was expected.")
        };
    }
}
