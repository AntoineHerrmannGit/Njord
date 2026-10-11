namespace Njord.Models.Instruments;

/// <summary>Represents a bond.</summary>
public sealed record Bond : FinancialInstrument
{
    #region Contract terms

    private string _currency = string.Empty;

    /// <summary>Gets the currency code for the face value.</summary>
    public required string Currency
    {
        get => _currency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _currency = value;
        }
    }

    /// <summary>Gets the bond face value in <see cref="Currency"/>.</summary>
    public required decimal FaceValue { get; init; }

    /// <summary>Gets the bond maturity date, when known.</summary>
    public DateOnly? MaturityDate { get; init; }

    /// <summary>Gets the annual coupon rate as a decimal fraction, when applicable.</summary>
    public decimal? CouponRate { get; init; }

    /// <summary>Gets the number of coupon payments per year, when applicable.</summary>
    public int? CouponPaymentsPerYear { get; init; }

    #endregion
}
