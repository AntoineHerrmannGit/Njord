using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Serialization;

namespace Njord.DataProviders.YahooFinance.Mapping;

internal sealed record YahooFinanceOhlcMappingInput
{
    #region Mapping input

    public required YahooFinanceChartResultDto Chart { get; init; }

    public required YahooFinanceResolvedOhlcRequest Request { get; init; }

    public required int MaximumBarsPerQuery { get; init; }

    #endregion
}
