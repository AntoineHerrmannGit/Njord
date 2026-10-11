namespace Njord.Core.EnumerableExtensions;

/// <summary>Provides simple null and emptiness checks for enumerable sequences.</summary>
public static class EnumerableExtensions
{
    #region Sequence checks

    /// <summary>Determines whether a sequence contains no elements.</summary>
    /// <typeparam name="T">The type of the sequence elements.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if the sequence is empty; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return !source.Any();
    }

    /// <summary>Determines whether a sequence contains at least one element.</summary>
    /// <typeparam name="T">The type of the sequence elements.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if the sequence contains an element; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static bool IsNotEmpty<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Any();
    }

    /// <summary>Determines whether a sequence is null or contains no elements.</summary>
    /// <typeparam name="T">The type of the sequence elements.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if the sequence is null or empty; otherwise, <see langword="false"/>.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null || !source.Any();

    /// <summary>Determines whether a sequence is not null and contains at least one element.</summary>
    /// <typeparam name="T">The type of the sequence elements.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if the sequence contains an element; otherwise, <see langword="false"/>.</returns>
    public static bool IsNotNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is not null && source.Any();

    /// <summary>Determines whether no element in the sequence satisfies the specified predicate.</summary>
    /// <typeparam name="T">The type of the sequence elements.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <param name="predicate">The condition to test against each element.</param>
    /// <returns>
    /// <see langword="true"/> if no element satisfies <paramref name="predicate"/> or the sequence is empty;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        return !source.Any(predicate);
    }

    #endregion
}
