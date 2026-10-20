namespace Njord.Data.Referential.Models;

/// <summary>Maps a canonical Njord ticker to a provider ticker over an inclusive validity period.</summary>
public sealed class TickerProviderMapping
{
    #region Mapping identity

    /// <summary>Gets the canonical Njord ticker.</summary>
    public required string Ticker { get; init; }

    /// <summary>Gets the provider name, for example Massive.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the ticker used by the provider.</summary>
    public required string ProviderTicker { get; init; }

    /// <summary>Gets the Njord asset class for the instrument.</summary>
    public required NjordAssetClass AssetClass { get; init; }

    #endregion

    #region Validity period

    /// <summary>Gets the first calendar quotation date covered by this mapping, inclusive; the time component is ignored.</summary>
    public DateTime StartDate { get; init; } = DateTime.MinValue;

    /// <summary>Gets the last calendar quotation date covered by this mapping, inclusive; the time component is ignored.</summary>
    public DateTime EndDate { get; init; } = DateTime.MaxValue;

    #endregion

    #region Validation

    /// <summary>Validates mapping fields and its inclusive quotation-date period.</summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Ticker);
        ArgumentException.ThrowIfNullOrWhiteSpace(Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(ProviderTicker);

        if (!string.Equals(Ticker, Ticker.Trim(), StringComparison.Ordinal) ||
            !string.Equals(Provider, Provider.Trim(), StringComparison.Ordinal) ||
            !string.Equals(ProviderTicker, ProviderTicker.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("Ticker and provider symbols must not have leading or trailing whitespace.");

        if (!Enum.IsDefined(AssetClass))
            throw new ArgumentOutOfRangeException(nameof(AssetClass), AssetClass, "The asset class is not supported.");

        if (DateOnly.FromDateTime(StartDate) > DateOnly.FromDateTime(EndDate))
            throw new ArgumentException("The mapping start date must not be after its end date.");
    }

    #endregion
}
