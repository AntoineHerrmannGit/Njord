namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains the comparison outcome and percentage difference for one OHLC field.</summary>
public sealed record MarketDataOhlcFieldComparison
{
    #region Field comparison

    /// <summary>Gets the OHLC field being compared.</summary>
    public required MarketDataOhlcField Field { get; init; }

    /// <summary>
    /// Gets the percentage difference, or null when exactly one value is zero or the
    /// comparison is inconclusive because its required metadata is unavailable.
    /// </summary>
    public decimal? PercentageDifference { get; init; }

    /// <summary>Gets the outcome for this field.</summary>
    public required MarketDataComparisonStatus Status { get; init; }

    #endregion
}
