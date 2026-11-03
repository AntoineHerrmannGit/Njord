using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Retrieves supported provider-neutral market-data types.</summary>
/// <typeparam name="TSupportedMarketData">The broad market-data type supported by this provider.</typeparam>
public interface IMarketDataProvider<TSupportedMarketData>
    where TSupportedMarketData : IMarketData
{
    /// <summary>Gets the stable, non-localized name identifying this provider.</summary>
    string ProviderName { get; }

    /// <summary>Retrieves one result per requested ticker for the requested market-data type.</summary>
    /// <typeparam name="TMarketData">The specific market-data type requested.</typeparam>
    /// <param name="request">The request for the specific market-data type.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Results containing the requested type and its diagnostics.</returns>
    /// <exception cref="NotSupportedException">
    /// The provider does not support the request and data-type combination.
    /// </exception>
    Task<IReadOnlyList<IMarketDataResult<TMarketData>>> Get<TMarketData>(
        IMarketDataRequest<TMarketData> request,
        CancellationToken cancellationToken = default)
        where TMarketData : TSupportedMarketData;
}
