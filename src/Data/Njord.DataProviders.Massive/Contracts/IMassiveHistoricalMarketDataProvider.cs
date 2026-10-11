using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Contracts;

/// <summary>Retrieves historical market data from Massive.</summary>
public interface IMassiveHistoricalMarketDataProvider
{
    #region Historical market data

    /// <summary>Retrieves historical OHLC bars for the requested ticker and date range.</summary>
    /// <param name="request">Ticker, asset class, date range, and aggregate interval.</param>
    /// <param name="cancellationToken">Token used to cancel the request and pagination.</param>
    /// <returns>The retrieved bars in chronological order.</returns>
    Task<IReadOnlyList<Ohlc>> GetHistoricalOhlcAsync(
        MassiveHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}
