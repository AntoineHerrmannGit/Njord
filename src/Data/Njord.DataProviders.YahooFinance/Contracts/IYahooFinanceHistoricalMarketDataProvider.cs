namespace Njord.DataProviders.YahooFinance.Contracts;

/// <summary>Retrieves historical market data from Yahoo Finance.</summary>
public interface IYahooFinanceHistoricalMarketDataProvider
{
    #region Historical market data

    /// <summary>Retrieves historical OHLC bars for canonical Njord tickers and a shared UTC date range.</summary>
    /// <param name="request">Canonical tickers, UTC date range, and aggregate interval.</param>
    /// <param name="cancellationToken">Token used to cancel requests and retry delays.</param>
    /// <returns>One result per requested ticker, including available bars and any provider/data diagnostics.</returns>
    Task<IReadOnlyList<YahooFinanceTickerOhlcResult>> GetHistoricalOhlcAsync(
        YahooFinanceHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}
