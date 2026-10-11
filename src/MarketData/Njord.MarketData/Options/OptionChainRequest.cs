namespace Njord.MarketData;

/// <summary>Describes an option-chain query for an underlying instrument.</summary>
public sealed record OptionChainRequest
{
    /// <summary>Creates an option-chain query.</summary>
    /// <param name="underlying">Equity or index underlying.</param>
    /// <param name="expiration">Optional expiration date; omitted to request the provider's default expiration.</param>
    public OptionChainRequest(MarketInstrument underlying, DateOnly? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(underlying);
        if (underlying.AssetClass is not (MarketAssetClass.Equity or MarketAssetClass.Index))
        {
            throw new ArgumentException(
                "Option chains require an equity or index underlying.",
                nameof(underlying));
        }

        Underlying = underlying;
        Expiration = expiration;
    }

    /// <summary>Gets the instrument underlying the option chain.</summary>
    public MarketInstrument Underlying { get; }

    /// <summary>Gets the requested expiration date, or <see langword="null"/> for the provider default.</summary>
    public DateOnly? Expiration { get; }
}
