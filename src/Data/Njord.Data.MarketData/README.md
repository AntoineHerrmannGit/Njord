# Njord.Data.MarketData

This .NET 10 project defines provider-neutral, generic market-data request,
result, and provider contracts alongside the legacy OHLC provider contract.
It references `Njord.Models` for the `IMarketData` marker and shared data types,
and does not implement or adapt a specific provider.

`IMarketDataRequest<TMarketData>` associates each request with its result data
type. Providers expose `Get<TMarketData>` through
`IMarketDataProvider<TSupportedMarketData>`; Massive and Yahoo Finance register
as `IMarketDataProvider<IMarketData>` and currently dispatch OHLC and corporate
action requests. Unsupported request/type combinations fail explicitly.
`IMarketDataResult<TMarketData>` exposes the canonical ticker, read-only data,
and common diagnostics. Existing OHLC and corporate-action results implement
the corresponding generic result while retaining their `Bars`, `Actions`, and
provider-specific properties; `Data` aliases their immutable snapshots.

```csharp
var provider = serviceProvider
    .GetServices<IMarketDataProvider<IMarketData>>()
    .Single(item => item.ProviderName == "YahooFinance");
var results = await provider.Get<Ohlc>(new MarketDataHistoricalOhlcRequest
{
    Tickers = ["AAPL"],
    From = new DateOnly(2025, 1, 1),
    To = new DateOnly(2025, 1, 31),
    Multiplier = 1,
    Interval = MarketDataInterval.Day
});

foreach (var result in results)
{
    foreach (var bar in result.Data)
        Console.WriteLine($"{result.Ticker}: {bar.Timestamp} {bar.Close}");

    foreach (var diagnostic in result.Diagnostics)
        Console.WriteLine($"{result.Ticker}: {diagnostic.Code} - {diagnostic.Message}");
}
```

Requests carry canonical ticker lists, inclusive `DateOnly` bounds, a positive
multiplier, and a provider-neutral interval. Each returned ticker result owns
read-only snapshots of its bars and diagnostics. `ExchangeTimeZoneId`, when
available, identifies the exchange timezone associated with that ticker; it
does not redefine the timezone or offset recorded by individual bars.

`MarketDataProviderSelector` provides explicit name-based selection over the
providers registered by the application. Provider names are matched
case-insensitively; duplicate or blank registered names fail during selector
construction, while looking up an unknown name throws `KeyNotFoundException`.
Both current adapters register as `IMarketDataProvider` for compatibility and
as the generic interface; their provider-owned contracts remain available.

Corporate actions are a separate optional capability exposed through
`ICorporateActionProvider`; an OHLC-only provider is not required to implement
it. The shared action kinds cover split, dividend, merger, ticker change,
delisting, and IPO. Dates, split-ratio representation, dividend details, and
other event metadata are nullable because providers may not supply them.
Provider-reported split-ratio text is retained without interpreting it or
deriving adjustment factors. This contract does not calculate adjusted prices.
Massive reports splits, dividends, ticker changes, and IPOs through available
endpoints; Yahoo Finance reports splits and dividends. Both report unsupported
action-type coverage explicitly.

`HistoricalOhlcComparisonService` is a pure comparison over two result vectors
and their common request. It uses a fixed 1% threshold, requires explicitly
unadjusted bars, equal known currencies, and valid exchange timezone metadata.
Daily bars match by exchange-local date; all other interval units match by
exact start instant (`PeriodStart`, falling back to `Timestamp`). It never
nearest-matches. Missing inputs or provider diagnostics make a ticker
inconclusive unless a matched field already proves a failure.
