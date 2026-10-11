namespace Njord.MarketData;

/// <summary>Describes an option contract and its available market quote fields.</summary>
public sealed record MarketOptionContract
{
    /// <summary>Creates an option contract snapshot.</summary>
    public MarketOptionContract(
        MarketInstrument instrument,
        MarketInstrument underlying,
        OptionType type,
        DateOnly expiration,
        decimal strike,
        string currency,
        string provider,
        decimal? lastPrice = null,
        decimal? bidPrice = null,
        decimal? askPrice = null,
        DateTimeOffset? lastTradeTimestamp = null,
        long? volume = null,
        long? openInterest = null,
        decimal? impliedVolatility = null,
        bool? isInTheMoney = null)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        ArgumentNullException.ThrowIfNull(underlying);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (instrument.AssetClass != MarketAssetClass.Option)
        {
            throw new ArgumentException("The contract instrument must have the option asset class.", nameof(instrument));
        }

        if (underlying.AssetClass is not (MarketAssetClass.Equity or MarketAssetClass.Index))
        {
            throw new ArgumentException("The underlying must be an equity or index.", nameof(underlying));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (strike <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(strike));
        }

        if (lastPrice < 0 || bidPrice < 0 || askPrice < 0 || impliedVolatility < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lastPrice), "Option prices and volatility cannot be negative.");
        }

        if (volume < 0 || openInterest < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume and open interest cannot be negative.");
        }

        Instrument = instrument;
        Underlying = underlying;
        Type = type;
        Expiration = expiration;
        Strike = strike;
        Currency = currency.Trim();
        Provider = provider.Trim();
        LastPrice = lastPrice;
        BidPrice = bidPrice;
        AskPrice = askPrice;
        LastTradeTimestamp = lastTradeTimestamp?.ToUniversalTime();
        Volume = volume;
        OpenInterest = openInterest;
        ImpliedVolatility = impliedVolatility;
        IsInTheMoney = isInTheMoney;
    }

    /// <summary>Gets the instrument identifier for the option contract.</summary>
    public MarketInstrument Instrument { get; }

    /// <summary>Gets the equity or index underlying this contract.</summary>
    public MarketInstrument Underlying { get; }

    /// <summary>Gets whether this contract is a call or put.</summary>
    public OptionType Type { get; }

    /// <summary>Gets the contract's expiration date.</summary>
    public DateOnly Expiration { get; }

    /// <summary>Gets the strike price in <see cref="Currency"/> per underlying unit.</summary>
    public decimal Strike { get; }

    /// <summary>Gets the most recent traded price, when available.</summary>
    public decimal? LastPrice { get; }

    /// <summary>Gets the current bid price, when available.</summary>
    public decimal? BidPrice { get; }

    /// <summary>Gets the current ask price, when available.</summary>
    public decimal? AskPrice { get; }

    /// <summary>Gets the timestamp of the last trade, when available.</summary>
    public DateTimeOffset? LastTradeTimestamp { get; }

    /// <summary>Gets the traded volume for the current session, when available.</summary>
    public long? Volume { get; }

    /// <summary>Gets the open interest, when available.</summary>
    public long? OpenInterest { get; }

    /// <summary>Gets the implied volatility as a decimal fraction, when available.</summary>
    public decimal? ImpliedVolatility { get; }

    /// <summary>Gets whether the contract is in the money, when provided.</summary>
    public bool? IsInTheMoney { get; }

    /// <summary>Gets the ISO currency code provided for the contract.</summary>
    public string Currency { get; }

    /// <summary>Gets the identifier of the data provider.</summary>
    public string Provider { get; }
}
