namespace Njord.Data.Referential.Errors;

/// <summary>Indicates that a ticker has no complete provider mapping for the requested period.</summary>
public sealed class TickerReferenceResolutionException : Exception
{
    #region Error details

    /// <summary>Initializes an exception describing an unresolved ticker period.</summary>
    public TickerReferenceResolutionException(string message)
        : base(message)
    {
    }

    #endregion
}
