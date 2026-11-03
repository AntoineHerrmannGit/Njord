namespace Njord.Data.MarketData.Contracts;

/// <summary>Identifies a provider-neutral corporate action category.</summary>
public enum CorporateActionKind
{
    Split,
    StockDividend,
    Dividend,
    Merger,
    TickerChange,
    Delisting,
    Ipo
}
