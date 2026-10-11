namespace Njord.MarketData;

/// <summary>Describes a historical market-data query.</summary>
public sealed record HistoricalMarketDataRequest
{
    /// <summary>Creates a historical market-data query over a half-open time range.</summary>
    public HistoricalMarketDataRequest(
        MarketInstrument instrument,
        DateTimeOffset start,
        DateTimeOffset end,
        MarketDataInterval interval)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        if (start >= end)
        {
            throw new ArgumentException("The start time must be earlier than the end time.", nameof(start));
        }

        if (!Enum.IsDefined(interval))
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        Instrument = instrument;
        Start = start;
        End = end;
        Interval = interval;
    }

    /// <summary>Gets the instrument to query.</summary>
    public MarketInstrument Instrument { get; }

    /// <summary>Gets the inclusive start instant.</summary>
    public DateTimeOffset Start { get; }

    /// <summary>Gets the exclusive end instant.</summary>
    public DateTimeOffset End { get; }

    /// <summary>Gets the interval for returned bars.</summary>
    public MarketDataInterval Interval { get; }
}
