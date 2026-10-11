namespace Njord.Models.MarketData;

/// <summary>Represents a rate point on a yield curve.</summary>
public sealed record YieldCurvePoint
{
    #region Curve data

    /// <summary>Gets the point maturity date.</summary>
    public required DateOnly MaturityDate { get; init; }

    /// <summary>Gets the rate as a decimal fraction.</summary>
    public required decimal Rate { get; init; }

    #endregion
}
