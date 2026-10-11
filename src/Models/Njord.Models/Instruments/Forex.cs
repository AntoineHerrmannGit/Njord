namespace Njord.Models.Instruments;

/// <summary>Represents a foreign-exchange currency pair.</summary>
public sealed record Forex : FinancialInstrument
{
    #region Currency pair

    private string _baseCurrency = string.Empty;
    private string _quoteCurrency = string.Empty;

    /// <summary>Gets the base currency code.</summary>
    public required string BaseCurrency
    {
        get => _baseCurrency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _baseCurrency = value;
        }
    }

    /// <summary>Gets the quote currency code.</summary>
    public required string QuoteCurrency
    {
        get => _quoteCurrency;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _quoteCurrency = value;
        }
    }

    #endregion
}
