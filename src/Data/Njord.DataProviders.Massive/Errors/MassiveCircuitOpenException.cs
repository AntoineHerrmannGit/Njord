namespace Njord.DataProviders.Massive.Errors;

/// <summary>Indicates that requests are temporarily blocked by the Massive circuit breaker.</summary>
public sealed class MassiveCircuitOpenException : MassiveDataProviderException
{
    #region Circuit breaker details

    public MassiveCircuitOpenException(TimeSpan retryAfter)
        : base("Massive requests are temporarily blocked by the circuit breaker.")
    {
        RetryAfter = retryAfter;
    }

    /// <summary>Gets the time remaining before a single probe request may be attempted.</summary>
    public TimeSpan RetryAfter { get; }

    #endregion
}
