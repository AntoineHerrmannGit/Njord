namespace Njord.Data.MarketData.Contracts;

/// <summary>
/// Optional corporate-action capability for a market data provider. Implementations
/// expose this separately so providers without corporate-action data need not support it.
/// </summary>
public interface ICorporateActionProvider : IMarketDataProvider
{
    #region Corporate actions

    /// <summary>Retrieves provider-reported corporate actions for a vector of canonical tickers.</summary>
    /// <param name="request">Canonical tickers and an inclusive date range.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>One result per requested ticker, including available actions and diagnostics.</returns>
    Task<IReadOnlyList<MarketDataTickerCorporateActionResult>> GetCorporateActionsAsync(
        CorporateActionHistoricalRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}
