namespace Njord.Models.MarketData;

/// <summary>Provides the common timestamp for standalone market data observations.</summary>
public abstract record MarketDataObservation : ITimestampedMarketData
{
    #region Observation identity

    /// <inheritdoc />
    public required DateTimeOffset Timestamp { get; init; }

    #endregion
}
