namespace Njord.Models.MarketData;

/// <summary>Identifies the source interpretation of a volatility observation.</summary>
public enum VolatilityKind
{
    /// <summary>Volatility estimated from observed historical price changes.</summary>
    Historical,

    /// <summary>Volatility implied by observed option prices.</summary>
    Implied
}
