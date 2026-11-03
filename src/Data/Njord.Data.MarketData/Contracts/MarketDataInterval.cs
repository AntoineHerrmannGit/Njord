namespace Njord.Data.MarketData.Contracts;

/// <summary>Identifies a provider-neutral historical OHLC interval unit.</summary>
public enum MarketDataInterval
{
    Second,
    Minute,
    Hour,
    Day,
    Session,
    Week,
    Month,
    Quarter,
    Year
}
