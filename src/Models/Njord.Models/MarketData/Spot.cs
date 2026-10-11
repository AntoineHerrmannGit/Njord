namespace Njord.Models.MarketData;

/// <summary>Represents a timestamped spot price observation.</summary>
public sealed record Spot : MarketDataObservation
{
    #region Spot price data

    private string _ticker = string.Empty;

    /// <summary>Gets the ticker identifying the instrument.</summary>
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <summary>Gets the observed spot price.</summary>
    public required decimal Price { get; init; }

    /// <summary>Gets the currency in which the price is quoted, when known.</summary>
    public string? Currency { get; init; }

    #endregion
}
