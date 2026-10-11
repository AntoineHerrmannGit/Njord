namespace Njord.DataProviders.Massive.Configuration;

/// <summary>Configuration owned by the Massive historical market-data provider.</summary>
public sealed class MassiveDataProviderOptions
{
    #region Provider configuration

    /// <summary>Gets the configuration section used by the Massive provider.</summary>
    public const string SectionName = "DataProviders:Massive";

    /// <summary>Gets or sets the Massive REST API base address.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>Gets or sets the API key from the local Massive configuration file.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum request-start rate for the configured account.</summary>
    public int RequestsPerMinute { get; set; } = 5;

    /// <summary>Gets or sets the timeout for an individual HTTP attempt.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the number of retries for transient GET failures.</summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>Gets or sets the base delay for exponential retry backoff.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Gets or sets the maximum exponential retry delay.</summary>
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the transient-failure threshold that opens the circuit.</summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>Gets or sets how long the circuit remains open before one probe is allowed.</summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum number of response pages per query.</summary>
    public int MaximumPagesPerQuery { get; set; } = 1000;

    #endregion
}
