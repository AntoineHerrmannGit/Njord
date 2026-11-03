namespace Njord.Data.MarketData.Contracts;

/// <summary>Retrieves provider-neutral historical market data.</summary>
public interface IMarketDataProvider
{
    #region Provider identity

    /// <summary>
    /// Gets the stable, non-localized name used to identify and select this provider.
    /// Names must remain consistent across application runs.
    /// </summary>
    string ProviderName { get; }

    #endregion

    #region Historical market data

    /// <summary>Retrieves historical OHLC bars for canonical tickers and a shared inclusive date range.</summary>
    /// <param name="request">Canonical tickers, inclusive date range, and aggregate interval.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>One result per requested ticker, including available bars and diagnostics.</returns>
    Task<IReadOnlyList<MarketDataTickerOhlcResult>> GetHistoricalOhlcAsync(
        MarketDataHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}
