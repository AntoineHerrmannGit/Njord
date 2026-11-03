using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Describes a vector historical OHLC request using canonical Njord tickers.</summary>
public sealed record MarketDataHistoricalOhlcRequest : IMarketDataRequest<Ohlc>
{
    #region Request identity

    /// <summary>Gets the canonical Njord tickers to retrieve.</summary>
    public required IReadOnlyList<string> Tickers { get; init; }

    #endregion

    #region Historical range

    /// <summary>Gets the first requested date, inclusive.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last requested date, inclusive.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Gets the number of interval units in each aggregate bar.</summary>
    public required int Multiplier { get; init; }

    /// <summary>Gets the provider-neutral unit used to construct each aggregate bar.</summary>
    public required MarketDataInterval Interval { get; init; }

    #endregion

    #region Validation

    /// <summary>Validates the common vector request values.</summary>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Tickers);
        if (Tickers.Count == 0)
            throw new ArgumentException("At least one Njord ticker is required.", nameof(Tickers));

        var uniqueTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ticker in Tickers)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
            if (!uniqueTickers.Add(ticker.Trim()))
                throw new ArgumentException($"Ticker '{ticker}' occurs more than once in the request.", nameof(Tickers));
        }

        if (!Enum.IsDefined(Interval))
            throw new ArgumentOutOfRangeException(nameof(Interval), Interval, "The interval is not supported.");

        if (From > To)
            throw new ArgumentException("The start date must not be after the end date.");

        if (To == DateOnly.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(To), To, "The end date must allow an exclusive next-day boundary.");

        if (Multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(Multiplier), Multiplier, "The multiplier must be positive.");
    }

    #endregion
}
