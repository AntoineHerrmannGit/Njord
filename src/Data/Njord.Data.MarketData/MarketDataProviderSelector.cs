using Njord.Data.MarketData.Contracts;

namespace Njord.Data.MarketData;

/// <summary>Selects a registered market data provider by its stable provider name.</summary>
public sealed class MarketDataProviderSelector
{
    #region Provider selection

    private readonly Dictionary<string, IMarketDataProvider> _providers;

    /// <summary>Initializes a selector from the providers registered with dependency injection.</summary>
    /// <param name="providers">Registered provider implementations.</param>
    /// <exception cref="InvalidOperationException">
    /// A registered provider has no name, or multiple providers use the same name.
    /// </exception>
    public MarketDataProviderSelector(IEnumerable<IMarketDataProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _providers = new Dictionary<string, IMarketDataProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            ArgumentNullException.ThrowIfNull(provider);
            var providerName = provider.ProviderName;
            if (string.IsNullOrWhiteSpace(providerName))
                throw new InvalidOperationException("A registered market data provider must have a non-empty provider name.");

            if (!_providers.TryAdd(providerName, provider))
                throw new InvalidOperationException(
                    $"More than one market data provider is registered with the name '{providerName}'.");
        }
    }

    /// <summary>Gets the registered provider with the requested stable name.</summary>
    /// <param name="providerName">Provider name, compared without regard to case.</param>
    /// <exception cref="ArgumentException"><paramref name="providerName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No provider has the requested name.</exception>
    public IMarketDataProvider GetProvider(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        if (_providers.TryGetValue(providerName, out var provider))
            return provider;

        throw new KeyNotFoundException($"No market data provider is registered with the name '{providerName}'.");
    }

    #endregion
}
