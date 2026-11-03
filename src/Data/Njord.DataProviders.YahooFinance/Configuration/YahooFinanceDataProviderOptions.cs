using Njord.Core.Retry;

namespace Njord.DataProviders.YahooFinance.Configuration;

/// <summary>Configuration owned by the Yahoo Finance historical market-data provider.</summary>
public sealed class YahooFinanceDataProviderOptions
{
    #region Provider configuration

    /// <summary>Gets the configuration section used by the Yahoo Finance provider.</summary>
    public const string SectionName = "DataProviders:YahooFinance";

    /// <summary>Gets or sets the HTTPS origin used by Yahoo Finance's chart endpoint.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>Gets or sets the timeout for an individual HTTP attempt.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum bars accepted from one chart response.</summary>
    public int MaximumBarsPerQuery { get; set; } = 100_000;

    /// <summary>Gets or sets the retry policy for transient Yahoo Finance failures.</summary>
    public RetryPolicyOptions Retry { get; set; } = new()
    {
        MaxRetryAttempts = 2,
        BaseDelay = TimeSpan.FromMilliseconds(500),
        MaximumDelay = TimeSpan.FromSeconds(30)
    };

    #endregion
}
