namespace Njord.Data.Referential.Errors;

/// <summary>Indicates conflicting mappings for the same canonical ticker and provider.</summary>
public sealed class TickerReferenceConflictException : Exception
{
    #region Error details

    /// <summary>Initializes an exception describing conflicting referential mappings.</summary>
    public TickerReferenceConflictException(string message)
        : base(message)
    {
    }

    #endregion
}
