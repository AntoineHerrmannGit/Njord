namespace Njord.Models.Instruments;

/// <summary>Represents a repurchase agreement.</summary>
public sealed record Repo : FinancialInstrument
{
    #region Contract terms

    private string _currency = string.Empty;

    /// <summary>Gets the currency code for the cash amount.</summary>
    public required string Currency
    {
        get => _currency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _currency = value;
        }
    }

    /// <summary>Gets the cash amount in <see cref="Currency"/>.</summary>
    public required decimal CashAmount { get; init; }

    /// <summary>Gets the ticker identifying the collateral, when known.</summary>
    public string? CollateralTicker { get; init; }

    /// <summary>Gets the agreement start date, when known.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Gets the agreement maturity date, when known.</summary>
    public DateOnly? MaturityDate { get; init; }

    /// <summary>Gets the repo rate as a decimal fraction, when supplied.</summary>
    public decimal? RepoRate { get; init; }

    #endregion
}
