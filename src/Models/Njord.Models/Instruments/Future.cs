namespace Njord.Models.Instruments;

/// <summary>Represents a futures contract.</summary>
public sealed record Future : FinancialInstrument
{
    #region Contract terms

    /// <summary>Gets the contract expiration date, when known.</summary>
    public DateOnly? ExpirationDate { get; init; }

    /// <summary>Gets the underlying instrument ticker, when known.</summary>
    public string? UnderlyingTicker { get; init; }

    /// <summary>Gets the contract size in the source-defined units, when supplied.</summary>
    public decimal? ContractSize { get; init; }

    #endregion
}
