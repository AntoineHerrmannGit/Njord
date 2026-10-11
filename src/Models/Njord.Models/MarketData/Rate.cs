namespace Njord.Models.MarketData;

/// <summary>Represents a timestamped market rate observation.</summary>
public sealed record Rate : MarketDataObservation
{
    #region Rate data

    private string _name = string.Empty;

    /// <summary>Gets the name identifying the rate.</summary>
    public required string Name
    {
        get => _name;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _name = value;
        }
    }

    /// <summary>Gets the rate as a decimal fraction (for example, 0.05 represents 5%).</summary>
    public required decimal Value { get; init; }

    /// <summary>Gets the associated currency code, when applicable.</summary>
    public string? Currency { get; init; }

    /// <summary>Gets the maturity date, when applicable.</summary>
    public DateOnly? MaturityDate { get; init; }

    #endregion
}
