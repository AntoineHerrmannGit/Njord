using Njord.Core.Mapping;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Errors;

namespace Njord.DataProviders.YahooFinance.Mapping;

internal sealed class YahooFinanceIntervalMapper :
    IMapper<YahooFinanceHistoricalOhlcRequest, YahooFinanceIntervalMapping>
{
    #region Interval mapping

    public YahooFinanceIntervalMapping Map(YahooFinanceHistoricalOhlcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Timespan switch
        {
            YahooFinanceTimespan.Minute => MapMinute(request.Multiplier),
            YahooFinanceTimespan.Hour when request.Multiplier == 1 => CreateMapping("60m", TimeSpan.FromHours(1)),
            YahooFinanceTimespan.Day when request.Multiplier is 1 or 5 =>
                CreateMapping($"{request.Multiplier}d", TimeSpan.FromDays(request.Multiplier)),
            YahooFinanceTimespan.Week when request.Multiplier == 1 => CreateMapping("1wk"),
            YahooFinanceTimespan.Month when request.Multiplier is 1 or 3 =>
                CreateMapping($"{request.Multiplier}mo"),
            YahooFinanceTimespan.Quarter when request.Multiplier == 1 => CreateMapping("3mo"),
            _ => throw new YahooFinanceDataProviderException(
                $"Yahoo Finance does not support the requested {request.Multiplier} {request.Timespan} interval.")
        };
    }

    private static YahooFinanceIntervalMapping MapMinute(int multiplier)
    {
        if (multiplier is not (1 or 2 or 5 or 15 or 30 or 60 or 90))
            throw new YahooFinanceDataProviderException(
                $"Yahoo Finance does not support {multiplier}-minute bars.");

        return CreateMapping($"{multiplier}m", TimeSpan.FromMinutes(multiplier));
    }

    private static YahooFinanceIntervalMapping CreateMapping(
        string yahooInterval,
        TimeSpan? reportedInterval = null) =>
        new()
        {
            YahooInterval = yahooInterval,
            ReportedInterval = reportedInterval
        };

    #endregion
}
