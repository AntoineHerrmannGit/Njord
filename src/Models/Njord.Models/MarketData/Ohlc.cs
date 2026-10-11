namespace Njord.Models.MarketData;

/// <summary>Represents timestamped open, high, low, and close market data.</summary>
public sealed record Ohlc : MarketDataObservation
{
    #region OHLC price data

    private string _ticker = string.Empty;
    private string _currency = string.Empty;
    private TimeSpan? _interval;

    /// <summary>Gets the ticker identifying the instrument.</summary>
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <summary>Gets the opening price.</summary>
    public required decimal Open { get; init; }

    /// <summary>Gets the highest price.</summary>
    public required decimal High { get; init; }

    /// <summary>Gets the lowest price.</summary>
    public required decimal Low { get; init; }

    /// <summary>Gets the closing price.</summary>
    public required decimal Close { get; init; }

    /// <summary>Gets the currency in which the prices are quoted.</summary>
    public required string Currency
    {
        get => _currency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _currency = value;
        }
    }

    /// <summary>Gets the bar interval when supplied by the data source.</summary>
    public TimeSpan? Interval
    {
        get => _interval;
        init
        {
            if (value.HasValue && value.Value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Interval), value, "The OHLC interval must be positive.");

            _interval = value;
        }
    }

    /// <summary>Gets the indicative start of the bar when supplied by the data source.</summary>
    public DateTimeOffset? PeriodStart { get; init; }

    /// <summary>Gets the indicative end of the bar when supplied by the data source.</summary>
    public DateTimeOffset? PeriodEnd { get; init; }

    #endregion
}
