# Njord.DataProviders.YahooFinance

Historical OHLC adapter for Yahoo Finance's chart endpoint. Yahoo response DTOs, HTTP transport, retry integration, and mapping remain inside this project; callers receive provider-neutral `Njord.Models.MarketData.Ohlc` records.

## Supported scope

The adapter accepts the six asset classes currently handled by the Massive provider: stocks, indices, options, futures, crypto, and forex. It sends a Yahoo ticker for each resolved mapping to the same chart endpoint; the adapter does not infer a Yahoo symbol from a canonical Njord ticker.

Mappings use provider name `YahooFinance`. Typical Yahoo symbols include `AAPL`, `^GSPC`, `BTC-USD`, `EURUSD=X`, and `ES=F`; the actual mapping must be checked against Yahoo Finance. Yahoo's historical chart availability varies by symbol and asset class. Yahoo does not reliably return historical OHLC for option contracts; an unavailable option ticker is reported as a per-ticker diagnostic and does not discard data retrieved for other tickers.

`YieldCurve`, `Rate`, `Swap`, `Bond`, and `Repo` are valid Njord referential classes but are not supported by this OHLC adapter. Such tickers receive an explicit unsupported-class diagnostic without a Yahoo request.

The batch result contains one `YahooFinanceTickerOhlcResult` per requested ticker, in request order. Each result includes available bars and diagnostics. Missing or invalid OHLC bars are omitted and counted in diagnostics; they are never interpolated or replaced. A ticker with no returned bars has an explicit `NoData` or provider diagnostic.

## Intervals and dates

Supported intervals are 1, 2, 5, 15, 30, 60, or 90 minutes; 1 hour; 1 or 5 days; 1 week; 1 or 3 months; and 1 quarter (mapped to 3 months). Other intervals are rejected before network dispatch. Yahoo may further restrict how far back intraday intervals can be requested; those provider errors are returned explicitly.

`From` and `To` are UTC calendar dates, both inclusive. The adapter sends Yahoo's `period1` at midnight on `From` and exclusive `period2` at midnight after `To`. Bar timestamps are returned as UTC `DateTimeOffset` values. The referential remains authoritative for canonical tickers, Yahoo symbols, asset classes, and mapping validity periods.

Yahoo's chart response provides OHLC, optional volume, optional currency, and an exchange timezone when the response includes it. It does not provide a reliable VWAP, transaction count, or explicit adjustment flag in the data mapped here; those model properties remain unset. Requests explicitly set `includeAdjustedClose=false`, so the separate adjusted-close series is not requested. This parameter does not prove that Yahoo's OHLC series is raw: its adjustment basis remains unknown, and a raw-price comparison must treat it as inconclusive unless raw prices are independently established. The provider does not reconstruct an as-traded series from adjusted data.

## Common provider and corporate-action contracts

`YahooFinanceHistoricalMarketDataProvider` is registered as
`IMarketDataProvider` and `ICorporateActionProvider` in addition to the
Yahoo-specific result contract. The common OHLC request accepts canonical
tickers and provider-neutral intervals; unsupported intervals are rejected
instead of being silently coerced. Applications that register multiple
providers can inject `MarketDataProviderSelector` and select one explicitly
by its registered name.

The optional corporate-action capability requests Yahoo chart `splits` and
`dividends` events and reports them as source-reported details. The response
also includes the provider's exchange timezone, used to map event timestamps
to exchange-local dates. Yahoo's chart endpoint does not supply the other
common action types through this request; the result includes an
`UnsupportedActionTypes` diagnostic. No price adjustments are calculated.

## Configuration and registration

The provider binds `DataProviders:YahooFinance` from its source-owned `yahoo-finance.settings.json`. The chart endpoint origin, request timeout, maximum accepted bars, and retry settings are validated at startup. Yahoo Finance chart access does not require an API key in this adapter.

```csharp
var yahooConfiguration = new ConfigurationBuilder()
    .AddJsonFile("yahoo-finance.settings.json", optional: false, reloadOnChange: false)
    .Build();
var referentialConfiguration = new ConfigurationBuilder()
    .AddJsonFile("referential.settings.local.json", optional: false, reloadOnChange: false)
    .Build();

services.AddNjordReferential(referentialConfiguration);
services.AddYahooFinanceDataProvider(yahooConfiguration);
```

Inject `IYahooFinanceHistoricalMarketDataProvider` and call `GetHistoricalOhlcAsync` with a `YahooFinanceHistoricalOhlcRequest` containing canonical `Tickers`, inclusive UTC `From`/`To` dates, `Multiplier`, and `Timespan`. Every ticker and requested date range must have complete `YahooFinance` mappings in the referential. Inspect each returned ticker result's `Diagnostics` before treating its bars as complete.

```csharp
var results = await yahooProvider.GetHistoricalOhlcAsync(new YahooFinanceHistoricalOhlcRequest
{
    Tickers = ["AAPL", "AAPL-2025-01-17-C-100"],
    From = new DateOnly(2025, 1, 1),
    To = new DateOnly(2025, 1, 31),
    Multiplier = 1,
    Timespan = YahooFinanceTimespan.Day
});

foreach (var result in results)
    foreach (var diagnostic in result.Diagnostics)
        Console.WriteLine($"{result.Ticker}: {diagnostic.Code} - {diagnostic.Message}");
```

## Operational limits

Yahoo Finance's chart endpoint is not a versioned commercial data contract and may change or apply symbol-, interval-, history-, or request-rate restrictions. The provider uses bounded retry for transient HTTP/network failures, propagates cancellation, and does not retry permanent HTTP or chart errors. Use the returned data subject to Yahoo Finance's terms and applicable licensing requirements.
