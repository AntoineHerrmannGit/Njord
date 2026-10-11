using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Serialization;

namespace Njord.DataProviders.Massive.Transport;

internal interface IMassiveAggregatesApiClient
{
    #region Aggregate retrieval

    Task<IReadOnlyList<MassiveAggregateResponseDto>> GetHistoricalAggregatesAsync(
        MassiveHistoricalOhlcRequest request,
        CancellationToken cancellationToken);

    #endregion
}
