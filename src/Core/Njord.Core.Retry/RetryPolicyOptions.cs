namespace Njord.Core.Retry;

/// <summary>Defines the retry count and delay limits for a <see cref="RetryPolicy"/>.</summary>
public sealed class RetryPolicyOptions
{
    #region Retry configuration

    /// <summary>Gets the number of retries after the initial attempt.</summary>
    public required int MaxRetryAttempts { get; init; }

    /// <summary>Gets the delay before the first retry.</summary>
    public required TimeSpan BaseDelay { get; init; }

    /// <summary>Gets the maximum exponential backoff delay.</summary>
    public required TimeSpan MaximumDelay { get; init; }

    #endregion
}
