namespace Njord.Models.MarketData;

/// <summary>Represents a timestamped yield curve.</summary>
public sealed record YieldCurve : MarketDataObservation
{
    #region Curve data

    private string _name = string.Empty;
    private string _currency = string.Empty;
    private IReadOnlyList<YieldCurvePoint> _points = Array.Empty<YieldCurvePoint>();

    /// <summary>Gets the name identifying the curve.</summary>
    public required string Name
    {
        get => _name;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _name = value;
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

    /// <summary>Gets the curve points provided by the source.</summary>
    public required IReadOnlyList<YieldCurvePoint> Points
    {
        get => _points;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _points = Array.AsReadOnly(value.ToArray());
        }
    }

    #endregion
}
