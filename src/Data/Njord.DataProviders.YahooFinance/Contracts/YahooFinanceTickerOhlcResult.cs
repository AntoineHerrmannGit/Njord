using Njord.Models.MarketData;

namespace Njord.DataProviders.YahooFinance.Contracts;

/// <summary>Contains the retrieved OHLC bars and diagnostics for one requested ticker.</summary>
public sealed record YahooFinanceTickerOhlcResult
{
    #region Ticker result

    private string _ticker = string.Empty;
    private IReadOnlyList<Ohlc> _bars = Array.Empty<Ohlc>();
    private IReadOnlyList<YahooFinanceDataDiagnostic> _diagnostics = Array.Empty<YahooFinanceDataDiagnostic>();

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

    /// <summary>Gets provider and data-quality diagnostics for this ticker.</summary>
    public required IReadOnlyList<YahooFinanceDataDiagnostic> Diagnostics
    {
        get => _diagnostics;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _diagnostics = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <summary>Gets whether at least one bar was retrieved.</summary>
    public bool HasData => Bars.Count > 0;

    #endregion
}
