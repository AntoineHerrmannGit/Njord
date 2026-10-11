namespace Njord.Models.MarketData;

/// <summary>Represents a calculated correlation between two ticker-based series.</summary>
public sealed record Correlation : MarketDataObservation
{
    #region Correlation data

    private string _firstTicker = string.Empty;
    private string _secondTicker = string.Empty;
    private double _value;

    /// <summary>Gets the ticker identifying the first series.</summary>
    public required string FirstTicker
    {
        get => _firstTicker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _firstTicker = value;
        }
    }

    /// <summary>Gets the ticker identifying the second series.</summary>
    public required string SecondTicker
    {
        get => _secondTicker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _secondTicker = value;
        }
    }

    /// <summary>Gets the correlation coefficient, bounded from -1 to 1.</summary>
    public required double Value
    {
        get => _value;
        init
        {
            if (!double.IsFinite(value) || value is < -1 or > 1)
                throw new ArgumentOutOfRangeException(nameof(Value), value, "Correlation must be finite and between -1 and 1.");

            _value = value;
        }
    }

    /// <summary>Gets whether the value is historical or implied correlation.</summary>
    public required CorrelationKind Kind { get; init; }

    /// <summary>Gets the statistical method used to determine the correlation.</summary>
    public required CorrelationMethod Method { get; init; }

    /// <summary>Gets the maturity associated with the correlation observation.</summary>
    public required DateOnly MaturityDate { get; init; }

    #endregion
}
