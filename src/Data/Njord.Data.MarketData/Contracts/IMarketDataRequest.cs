using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Marks a request for a specific provider-neutral market-data type.</summary>
/// <typeparam name="TMarketData">The requested market-data value type.</typeparam>
public interface IMarketDataRequest<TMarketData>
    where TMarketData : IMarketData
{
}
