using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains provider-reported details for one corporate action.</summary>
public sealed record MarketCorporateAction : IMarketData
{
    #region Corporate action details

    /// <summary>Gets the provider-neutral corporate action category.</summary>
    public required CorporateActionKind Kind { get; init; }

    /// <summary>Gets the provider-reported execution date, when available.</summary>
    public DateOnly? ExecutionDate { get; init; }

    /// <summary>Gets the provider-reported ex-dividend date, when available.</summary>
    public DateOnly? ExDividendDate { get; init; }

    /// <summary>Gets the provider-reported effective date, when available.</summary>
    public DateOnly? EffectiveDate { get; init; }

    /// <summary>Gets the provider-reported announcement date, when available.</summary>
    public DateOnly? AnnouncementDate { get; init; }

    /// <summary>Gets the provider-reported record date, when available.</summary>
    public DateOnly? RecordDate { get; init; }

    /// <summary>Gets the provider-reported payment date, when available.</summary>
    public DateOnly? PayDate { get; init; }

    /// <summary>
    /// Gets the provider-reported split ratio representation, when available.
    /// The value is preserved as reported and is not interpreted as an adjustment factor.
    /// </summary>
    public string? SplitRatio { get; init; }

    /// <summary>Gets the provider-reported old-share count for a split, when available.</summary>
    public decimal? SplitFrom { get; init; }

    /// <summary>Gets the provider-reported new-share count for a split, when available.</summary>
    public decimal? SplitTo { get; init; }

    /// <summary>Gets the provider-reported dividend amount, when available.</summary>
    public decimal? DividendAmount { get; init; }

    /// <summary>Gets the currency associated with the dividend amount, when available.</summary>
    public string? DividendCurrency { get; init; }

    /// <summary>Gets the related canonical or provider-reported ticker, when available.</summary>
    public string? RelatedTicker { get; init; }

    /// <summary>Gets the provider-reported event description, when available.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the provider-reported event status, when available.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the provider-reported issuer name, when available.</summary>
    public string? IssuerName { get; init; }

    /// <summary>Gets the provider-reported primary exchange, when available.</summary>
    public string? PrimaryExchange { get; init; }

    /// <summary>Gets the provider-reported currency code associated with the event, when available.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>Gets the provider-specific event type, when available.</summary>
    public string? ProviderActionType { get; init; }

    /// <summary>Gets the provider's event identifier, when available.</summary>
    public string? EventId { get; init; }

    #endregion
}
