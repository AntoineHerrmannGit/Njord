namespace Njord.MarketData;

/// <summary>Identifies a market instrument independently of a data provider.</summary>
public sealed record MarketInstrument
{
    /// <summary>Creates an instrument identifier.</summary>
    public MarketInstrument(string symbol, MarketAssetClass assetClass, string? exchange = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (!Enum.IsDefined(assetClass))
        {
            throw new ArgumentOutOfRangeException(nameof(assetClass));
        }

        Symbol = symbol.Trim();
        AssetClass = assetClass;
        Exchange = string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim();
    }

    /// <summary>Gets the instrument's provider-independent symbol.</summary>
    public string Symbol { get; }

    /// <summary>Gets the instrument's asset class.</summary>
    public MarketAssetClass AssetClass { get; }

    /// <summary>Gets the exchange when known.</summary>
    public string? Exchange { get; }
}
