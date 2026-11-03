namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains corporate actions and diagnostics for one requested ticker.</summary>
public sealed record MarketDataTickerCorporateActionResult : IMarketDataResult<MarketCorporateAction>
{
    #region Ticker result

    private string _ticker = string.Empty;
    private IReadOnlyList<MarketCorporateAction> _actions = Array.Empty<MarketCorporateAction>();
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

    /// <summary>Gets the provider-reported corporate actions for this ticker.</summary>
    public required IReadOnlyList<MarketCorporateAction> Actions
    {
        get => _actions;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _actions = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<MarketCorporateAction> Data => Actions;

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

    /// <summary>Gets whether at least one corporate action was retrieved.</summary>
    public bool HasData => Actions.Count > 0;

    #endregion
}
