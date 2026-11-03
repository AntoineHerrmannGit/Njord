namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains the OHLC comparison outcome for one requested ticker.</summary>
public sealed record MarketDataTickerOhlcComparisonResult
{
    #region Ticker comparison result

    private string _ticker = string.Empty;
    private IReadOnlyList<MarketDataOhlcBarComparison> _matchedBars = Array.Empty<MarketDataOhlcBarComparison>();
    private IReadOnlyList<MarketDataComparisonDiagnostic> _diagnostics = Array.Empty<MarketDataComparisonDiagnostic>();

    /// <summary>Gets the canonical Njord ticker from the comparison request.</summary>
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <summary>Gets the number of bars supplied by the first provider.</summary>
    public required int LeftBarCount { get; init; }

    /// <summary>Gets the number of bars supplied by the second provider.</summary>
    public required int RightBarCount { get; init; }

    /// <summary>Gets the number of unique bar pairs matched between providers.</summary>
    public required int MatchedBarCount { get; init; }

    /// <summary>Gets the number of matched bar pairs for which all comparison prerequisites were met.</summary>
    public required int ComparedBarCount { get; init; }

    /// <summary>Gets the matched bar comparisons.</summary>
    public required IReadOnlyList<MarketDataOhlcBarComparison> MatchedBars
    {
        get => _matchedBars;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _matchedBars = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <summary>Gets the overall validation outcome for this ticker.</summary>
    public required MarketDataComparisonStatus Status { get; init; }

    /// <summary>Gets diagnostics explaining incomplete or invalid comparison inputs.</summary>
    public required IReadOnlyList<MarketDataComparisonDiagnostic> Diagnostics
    {
        get => _diagnostics;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _diagnostics = Array.AsReadOnly(value.ToArray());
        }
    }

    #endregion
}
