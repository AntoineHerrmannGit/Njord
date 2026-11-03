using Njord.DataProviders.YahooFinance.Contracts;
using Njord.Models.MarketData;

namespace Njord.DataProviders.YahooFinance.Mapping;

internal sealed record YahooFinanceOhlcMappingResult
{
    #region Mapping result

    public required IReadOnlyList<Ohlc> Bars { get; init; }

    public required IReadOnlyList<YahooFinanceDataDiagnostic> Diagnostics { get; init; }

    #endregion
}
