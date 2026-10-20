using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Contracts;

/// <summary>Retrieves historical market data from Massive.</summary>
public interface IMassiveHistoricalMarketDataProvider
{
    #region Historical market data

    /// <summary>Retrieves historical OHLC bars for a vector of canonical Njord tickers and a shared date range.</summary>
    /// <param name="request">Canonical tickers, date range, and aggregate interval.</param>
    /// <param name="cancellationToken">Token used to cancel the request and pagination.</param>
    /// <returns>The retrieved bars grouped in request ticker order and chronological order per ticker.</returns>
    Task<IReadOnlyList<Ohlc>> GetHistoricalOhlcAsync(
        MassiveHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}
