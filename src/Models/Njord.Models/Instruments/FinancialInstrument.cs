namespace Njord.Models.Instruments;

/// <summary>Provides the common identity and timestamp for ticker-based instruments.</summary>
public abstract record FinancialInstrument : IFinancialInstrument
{
    #region Instrument identity

    private string _ticker = string.Empty;

    /// <inheritdoc />
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <inheritdoc />
    public required DateTimeOffset Timestamp { get; init; }

    #endregion
}
