namespace Njord.Models.MarketData;

/// <summary>Identifies whether a correlation is historical or implied.</summary>
public enum CorrelationKind
{
    /// <summary>Correlation estimated from observed historical data.</summary>
    Historical,

    /// <summary>Correlation implied by market prices of correlation-sensitive instruments.</summary>
    Implied
}
