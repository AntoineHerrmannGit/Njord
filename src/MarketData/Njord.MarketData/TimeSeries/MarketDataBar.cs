namespace Njord.MarketData;

/// <summary>A provider-neutral OHLCV bar for a market instrument.</summary>
public sealed record MarketDataBar
{
    /// <summary>Creates a validated OHLCV bar.</summary>
    public MarketDataBar(
        MarketInstrument instrument,
        DateTimeOffset timestamp,
        MarketDataInterval interval,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal? volume,
        string currency,
        string provider,
        decimal? adjustedClose = null)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (!Enum.IsDefined(interval))
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        if (high < low || high < open || high < close || low > open || low > close)
        {
            throw new ArgumentException("OHLC values must satisfy low <= open/close <= high.");
        }

        if (volume < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(volume));
        }

        Instrument = instrument;
        Timestamp = timestamp.ToUniversalTime();
        Interval = interval;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        Currency = currency.Trim();
        Provider = provider.Trim();
        AdjustedClose = adjustedClose;
    }

    /// <summary>Gets the instrument represented by this bar.</summary>
    public MarketInstrument Instrument { get; }

    /// <summary>Gets the bar timestamp as a UTC instant.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the bar interval.</summary>
    public MarketDataInterval Interval { get; }

    /// <summary>Gets the opening price.</summary>
    public decimal Open { get; }

    /// <summary>Gets the highest price.</summary>
    public decimal High { get; }

    /// <summary>Gets the lowest price.</summary>
    public decimal Low { get; }

    /// <summary>Gets the unadjusted closing price.</summary>
    public decimal Close { get; }

    /// <summary>Gets the traded volume when supplied by the provider.</summary>
    public decimal? Volume { get; }

    /// <summary>Gets the adjusted closing price when supplied by the provider.</summary>
    public decimal? AdjustedClose { get; }

    /// <summary>Gets the ISO currency code supplied by the provider.</summary>
    public string Currency { get; }

    /// <summary>Gets the identifier of the data provider.</summary>
    public string Provider { get; }
}
