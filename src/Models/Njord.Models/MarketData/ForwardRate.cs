namespace Njord.Models.MarketData;

/// <summary>Represents a calculated forward rate over a future accrual period.</summary>
public sealed record ForwardRate : MarketDataObservation
{
    #region Forward rate data

    private string _curveName = string.Empty;
    private string _currency = string.Empty;
    private double _value;

    /// <summary>Gets the name identifying the curve used for the rate.</summary>
    public required string CurveName
    {
        get => _curveName;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _curveName = value;
        }
    }

    /// <summary>Gets the currency associated with the curve.</summary>
    public required string Currency
    {
        get => _currency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _currency = value;
        }
    }

    /// <summary>Gets the rate as a decimal fraction, such as 0.05 for 5%.</summary>
    public required double Value
    {
        get => _value;
        init
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(Value), value, "Forward rate must be finite.");

            _value = value;
        }
    }

    /// <summary>Gets the start date of the forward accrual period.</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Gets the end date of the forward accrual period.</summary>
    public required DateOnly EndDate { get; init; }

    #endregion
}
