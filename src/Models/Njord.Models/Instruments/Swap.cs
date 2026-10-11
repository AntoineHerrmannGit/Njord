namespace Njord.Models.Instruments;

/// <summary>Represents a swap contract with common notional and date terms.</summary>
public sealed record Swap : FinancialInstrument
{
    #region Contract terms

    private string _currency = string.Empty;

    /// <summary>Gets the currency code for the notional.</summary>
    public required string Currency
    {
        get => _currency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _currency = value;
        }
    }

    /// <summary>Gets the notional amount in <see cref="Currency"/>.</summary>
    public required decimal Notional { get; init; }

    /// <summary>Gets the contract effective date, when known.</summary>
    public DateOnly? EffectiveDate { get; init; }

    /// <summary>Gets the contract maturity date, when known.</summary>
    public DateOnly? MaturityDate { get; init; }

    /// <summary>Gets the fixed rate as a decimal fraction, when applicable.</summary>
    public decimal? FixedRate { get; init; }

    /// <summary>Gets the floating-rate index name, when applicable.</summary>
    public string? FloatingRateIndex { get; init; }

    #endregion
}
