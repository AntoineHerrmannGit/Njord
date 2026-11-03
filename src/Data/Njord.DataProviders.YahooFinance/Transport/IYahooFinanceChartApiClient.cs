using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Serialization;

namespace Njord.DataProviders.YahooFinance.Transport;

internal interface IYahooFinanceChartApiClient
{
    #region Chart retrieval

    Task<YahooFinanceChartResultDto> GetHistoricalChartAsync(
        YahooFinanceResolvedOhlcRequest request,
        CancellationToken cancellationToken);

    #endregion
}
