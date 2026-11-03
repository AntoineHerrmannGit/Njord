namespace Njord.DataProviders.YahooFinance.Mapping;

internal sealed record YahooFinanceIntervalMapping
{
    #region Interval mapping

    public required string YahooInterval { get; init; }

    public TimeSpan? ReportedInterval { get; init; }

    #endregion
}
