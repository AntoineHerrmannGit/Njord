namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains the matched bar starts and OHLC field comparisons for one bar pair.</summary>
public sealed record MarketDataOhlcBarComparison
{
    #region Matched bar comparison

    private IReadOnlyList<MarketDataOhlcFieldComparison> _fields = Array.Empty<MarketDataOhlcFieldComparison>();

    /// <summary>Gets the first provider's bar-start instant.</summary>
    public required DateTimeOffset LeftBarStart { get; init; }

    /// <summary>Gets the second provider's bar-start instant.</summary>
    public required DateTimeOffset RightBarStart { get; init; }

    /// <summary>Gets the exchange-local date used as the key for daily bars, when applicable.</summary>
    public DateOnly? ExchangeLocalDate { get; init; }

    /// <summary>Gets the Open, High, Low, and Close field comparisons.</summary>
    public required IReadOnlyList<MarketDataOhlcFieldComparison> Fields
    {
        get => _fields;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _fields = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <summary>Gets the outcome for this matched bar pair.</summary>
    public MarketDataComparisonStatus Status
    {
        get
        {
            if (Fields.Any(comparison => comparison.Status == MarketDataComparisonStatus.Fail))
                return MarketDataComparisonStatus.Fail;

            if (Fields.Any(comparison => comparison.Status == MarketDataComparisonStatus.Inconclusive))
                return MarketDataComparisonStatus.Inconclusive;

            return MarketDataComparisonStatus.Pass;
        }
    }

    #endregion
}
