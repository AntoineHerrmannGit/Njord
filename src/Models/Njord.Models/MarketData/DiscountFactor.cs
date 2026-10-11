namespace Njord.Models.MarketData;

/// <summary>Represents a calculated discount factor from a named curve.</summary>
public sealed record DiscountFactor : MarketDataObservation
{
    #region Discount factor data

    private string _curveName = string.Empty;
    private string _currency = string.Empty;
    private double _value;

    /// <summary>Gets the name identifying the curve used for the discount factor.</summary>
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

    /// <summary>Gets the dimensionless discount factor.</summary>
    public required double Value
    {
        get => _value;
        init
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(Value), value, "Discount factor must be finite and positive.");

            _value = value;
        }
    }

    /// <summary>Gets the date to which the discount factor applies.</summary>
    public required DateOnly MaturityDate { get; init; }

    #endregion
}
