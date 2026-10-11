namespace Njord.Models.MarketData;

/// <summary>Represents a calculated volatility observation for a ticker-based series.</summary>
public sealed record Volatility : MarketDataObservation
{
    #region Volatility data

    private string _ticker = string.Empty;
    private double _value;

    /// <summary>Gets the ticker identifying the underlying series.</summary>
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <summary>Gets the volatility as a decimal fraction, such as 0.2 for 20%.</summary>
    public required double Value
    {
        get => _value;
        init
        {
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(Value), value, "Volatility must be finite and non-negative.");

            _value = value;
        }
    }

    /// <summary>Gets whether the value is historical or implied volatility.</summary>
    public required VolatilityKind Kind { get; init; }

    /// <summary>Gets the maturity associated with the volatility observation.</summary>
    public required DateOnly MaturityDate { get; init; }

    /// <summary>Gets whether the volatility value is annualized.</summary>
    public required bool IsAnnualized { get; init; }

    #endregion
}
