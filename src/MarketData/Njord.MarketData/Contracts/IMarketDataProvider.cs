namespace Njord.MarketData;

/// <summary>Retrieves market data from a provider.</summary>
public interface IMarketDataProvider
{
    /// <summary>Gets the stable identifier of this provider.</summary>
    string ProviderId { get; }

    /// <summary>Gets historical OHLCV bars for an instrument.</summary>
    /// <param name="request">Instrument, time range, and bar interval to retrieve.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Bars in chronological order; an empty collection means no bars were available.</returns>
    Task<IReadOnlyList<MarketDataBar>> GetHistoricalBarsAsync(
        HistoricalMarketDataRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the latest available quote for an instrument.</summary>
    /// <param name="instrument">Instrument to retrieve.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The latest quote available from this provider.</returns>
    Task<MarketDataQuote> GetQuoteAsync(
        MarketInstrument instrument,
        CancellationToken cancellationToken = default);

    /// <summary>Gets an option-chain snapshot for an equity or index underlying.</summary>
    /// <param name="request">Underlying instrument and optional expiration date.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The contracts for the selected expiration and the available expiration dates.</returns>
    Task<MarketOptionChain> GetOptionChainAsync(
        OptionChainRequest request,
        CancellationToken cancellationToken = default);
}
