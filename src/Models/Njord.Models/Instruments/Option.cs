namespace Njord.Models.Instruments;

/// <summary>Represents an option contract.</summary>
public sealed record Option : FinancialInstrument
{
    #region Contract terms

    private string _underlyingTicker = string.Empty;

    /// <summary>Gets the ticker identifying the underlying instrument.</summary>
    public required string UnderlyingTicker
    {
        get => _underlyingTicker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _underlyingTicker = value;
        }
    }

    /// <summary>Gets the option expiration date.</summary>
    public required DateOnly ExpirationDate { get; init; }

    /// <summary>Gets whether the option is a call or a put.</summary>
    public required OptionType Type { get; init; }

    /// <summary>Gets the strike in the instrument's quoted units.</summary>
    public required decimal Strike { get; init; }

    #endregion
}
