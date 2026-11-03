using Njord.DataProviders.Massive.Serialization;

namespace Njord.DataProviders.Massive.Transport;

internal interface IMassiveCorporateActionsApiClient
{
    #region Corporate action retrieval

    Task<IReadOnlyList<MassiveStockSplitDto>> GetHistoricalStockSplitsAsync(
        string ticker,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MassiveStockDividendDto>> GetHistoricalStockDividendsAsync(
        string ticker,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MassiveStockIpoDto>> GetHistoricalStockIposAsync(
        string ticker,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MassiveTickerEventDto>> GetTickerEventsAsync(
        string ticker,
        CancellationToken cancellationToken);

    #endregion
}
