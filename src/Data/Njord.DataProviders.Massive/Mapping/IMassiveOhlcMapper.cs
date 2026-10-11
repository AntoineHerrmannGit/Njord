using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Serialization;
using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Mapping;

internal interface IMassiveOhlcMapper
{
    #region Market data mapping

    IReadOnlyList<Ohlc> Map(
        IReadOnlyList<MassiveAggregateResponseDto> pages,
        MassiveHistoricalOhlcRequest request);

    #endregion
}
