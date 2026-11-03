namespace Njord.Core.Mapping;

/// <summary>Maps a source value to a target value.</summary>
/// <typeparam name="TSource">The source value type.</typeparam>
/// <typeparam name="TTarget">The target value type.</typeparam>
public interface IMapper<in TSource, out TTarget>
{
    #region Mapping

    /// <summary>Maps the specified source value to a target value.</summary>
    /// <param name="source">The value to map.</param>
    /// <returns>The mapped target value.</returns>
    TTarget Map(TSource source);

    #endregion
}
