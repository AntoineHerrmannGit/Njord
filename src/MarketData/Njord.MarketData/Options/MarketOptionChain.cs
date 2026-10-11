namespace Njord.MarketData;

/// <summary>A snapshot of option contracts for one expiration and underlying.</summary>
public sealed record MarketOptionChain
{
    /// <summary>Creates an option-chain snapshot.</summary>
    public MarketOptionChain(
        MarketInstrument underlying,
        DateOnly selectedExpiration,
        IEnumerable<DateOnly> availableExpirations,
        IEnumerable<MarketOptionContract> contracts,
        string provider)
    {
        ArgumentNullException.ThrowIfNull(underlying);
        ArgumentNullException.ThrowIfNull(availableExpirations);
        ArgumentNullException.ThrowIfNull(contracts);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (underlying.AssetClass is not (MarketAssetClass.Equity or MarketAssetClass.Index))
        {
            throw new ArgumentException("The underlying must be an equity or index.", nameof(underlying));
        }

        var expirations = availableExpirations.Distinct().Order().ToArray();
        if (!expirations.Contains(selectedExpiration))
        {
            throw new ArgumentException(
                "The selected expiration must be present in the available expirations.",
                nameof(selectedExpiration));
        }

        var optionContracts = contracts.ToArray();
        if (optionContracts.Any(contract =>
                contract.Underlying != underlying || contract.Expiration != selectedExpiration))
        {
            throw new ArgumentException(
                "Every contract must match the chain underlying and selected expiration.",
                nameof(contracts));
        }

        Underlying = underlying;
        SelectedExpiration = selectedExpiration;
        AvailableExpirations = Array.AsReadOnly(expirations);
        Contracts = Array.AsReadOnly(optionContracts);
        Provider = provider.Trim();
    }

    /// <summary>Gets the equity or index underlying this chain describes.</summary>
    public MarketInstrument Underlying { get; }

    /// <summary>Gets the expiration selected for this chain snapshot.</summary>
    public DateOnly SelectedExpiration { get; }

    /// <summary>Gets all expiration dates reported by the provider.</summary>
    public IReadOnlyList<DateOnly> AvailableExpirations { get; }

    /// <summary>Gets calls and puts for the selected expiration.</summary>
    public IReadOnlyList<MarketOptionContract> Contracts { get; }

    /// <summary>Gets the identifier of the data provider.</summary>
    public string Provider { get; }
}
