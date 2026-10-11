namespace Njord.MarketData;

/// <summary>A provider-neutral market quote for an instrument.</summary>
public sealed record MarketDataQuote
{
    /// <summary>Creates a market quote snapshot.</summary>
    public MarketDataQuote(
        MarketInstrument instrument,
        DateTimeOffset timestamp,
        decimal lastPrice,
        string currency,
        string provider,
        decimal? bidPrice = null,
        decimal? askPrice = null,
        decimal? previousClose = null)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        Instrument = instrument;
        Timestamp = timestamp.ToUniversalTime();
        LastPrice = lastPrice;
        Currency = currency.Trim();
        Provider = provider.Trim();
        BidPrice = bidPrice;
        AskPrice = askPrice;
        PreviousClose = previousClose;
    }

    /// <summary>Gets the instrument represented by this quote.</summary>
    public MarketInstrument Instrument { get; }

    /// <summary>Gets the quote timestamp as a UTC instant.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the provider's latest market price.</summary>
    public decimal LastPrice { get; }

    /// <summary>Gets the bid price when supplied by the provider.</summary>
    public decimal? BidPrice { get; }

    /// <summary>Gets the ask price when supplied by the provider.</summary>
    public decimal? AskPrice { get; }

    /// <summary>Gets the previous closing price when supplied by the provider.</summary>
    public decimal? PreviousClose { get; }

    /// <summary>Gets the ISO currency code supplied by the provider.</summary>
    public string Currency { get; }

    /// <summary>Gets the identifier of the data provider.</summary>
    public string Provider { get; }
}
