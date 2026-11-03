using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains historical OHLC bars and diagnostics for one requested ticker.</summary>
public sealed record MarketDataTickerOhlcResult : IMarketDataResult<Ohlc>
{
    #region Ticker result

    private string _ticker = string.Empty;
    private IReadOnlyList<Ohlc> _bars = Array.Empty<Ohlc>();
    private IReadOnlyList<MarketDataDiagnostic> _diagnostics = Array.Empty<MarketDataDiagnostic>();

    /// <summary>Gets the canonical Njord ticker from the request.</summary>
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <summary>Gets the available OHLC bars in chronological order.</summary>
    public required IReadOnlyList<Ohlc> Bars
    {
        get => _bars;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _bars = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Ohlc> Data => Bars;

    /// <summary>Gets provider and data-quality diagnostics for this ticker.</summary>
    public required IReadOnlyList<MarketDataDiagnostic> Diagnostics
    {
        get => _diagnostics;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _diagnostics = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <summary>
    /// Gets the provider's exchange timezone identifier, when available. It describes the
    /// exchange timezone associated with the ticker, not the timezone or offset of each bar.
    /// </summary>
    public string? ExchangeTimeZoneId { get; init; }

    /// <summary>Gets whether at least one bar was retrieved.</summary>
    public bool HasData => Bars.Count > 0;

    #endregion
}
