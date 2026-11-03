namespace Njord.DataProviders.YahooFinance.Contracts;

internal sealed record YahooFinanceResolvedOhlcRequest
{
    #region Ticker resolution

    public required string NjordTicker { get; init; }

    public required string Ticker { get; init; }

    #endregion

    #region Historical range

    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    public required string Interval { get; init; }

    public TimeSpan? ReportedInterval { get; init; }

    #endregion
}
