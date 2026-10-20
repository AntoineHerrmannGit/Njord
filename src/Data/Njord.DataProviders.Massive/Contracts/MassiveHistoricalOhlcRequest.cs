namespace Njord.DataProviders.Massive.Contracts;

/// <summary>Describes a vector historical OHLC request using canonical Njord tickers.</summary>
public sealed record MassiveHistoricalOhlcRequest
{
    #region Request identity

    /// <summary>Gets the Njord tickers to retrieve.</summary>
    public required IReadOnlyList<string> Tickers { get; init; }

    #endregion

    #region Historical range

    /// <summary>Gets the first requested date, inclusive.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last requested date, inclusive.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Gets the number of time units in each aggregate bar.</summary>
    public required int Multiplier { get; init; }

    /// <summary>Gets the time unit used to construct each aggregate bar.</summary>
    public required MassiveTimespan Timespan { get; init; }

    /// <summary>Gets whether split-adjusted data should be requested when supported.</summary>
    public bool? Adjusted { get; init; }

    #endregion

    #region Validation

    /// <summary>Validates the shared vector request values.</summary>
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
            {
                throw new ArgumentException($"Ticker '{ticker}' occurs more than once in the request.", nameof(Tickers));
            }
        }

        if (!Enum.IsDefined(Timespan))
            throw new ArgumentOutOfRangeException(nameof(Timespan), Timespan, "The timespan is not supported.");

        if (From > To)
            throw new ArgumentException("The start date must not be after the end date.");

        if (Multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(Multiplier), Multiplier, "The multiplier must be positive.");
    }

    #endregion
}
