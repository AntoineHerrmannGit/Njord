using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Contains market data and diagnostics for one canonical ticker.</summary>
/// <typeparam name="TMarketData">The market-data value type contained by the result.</typeparam>
public interface IMarketDataResult<out TMarketData>
    where TMarketData : IMarketData
{
    /// <summary>Gets the canonical Njord ticker from the request.</summary>
    string Ticker { get; }

    /// <summary>Gets the returned market data in provider/request order.</summary>
    IReadOnlyList<TMarketData> Data { get; }

    /// <summary>Gets provider and data-quality diagnostics for this ticker.</summary>
    IReadOnlyList<MarketDataDiagnostic> Diagnostics { get; }
}
