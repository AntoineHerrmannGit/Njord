namespace Njord.Models.MarketData;

/// <summary>Represents market data captured at a specific point in time.</summary>
public interface ITimestampedMarketData
{
    #region Market data identity

    /// <summary>Gets the timestamp associated with this market data.</summary>
    DateTimeOffset Timestamp { get; }

    #endregion
}
