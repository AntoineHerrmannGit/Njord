# Njord.DataProviders.Massive

Historical OHLC adapter for Massive. Vendor response DTOs, HTTP transport, retry/resilience integration, and mapping remain inside this project; callers receive provider-neutral `Njord.Models.MarketData.Ohlc` records.

## Supported historical aggregates

The provider currently retrieves aggregate bars for stocks, indices, options, crypto, forex, and futures. The first five asset classes use Massive's `/v2/aggs/ticker/.../range/...` endpoint; futures use `/futures/v1/aggs/{ticker}` and its `resolution`/`window_start` query format. Futures timestamps are nanoseconds; other aggregate timestamps are milliseconds. All timestamps are converted to UTC `DateTimeOffset`.

Callers submit one or more canonical Njord tickers. The adapter resolves each ticker, its Njord asset class, its Massive ticker, and the requested period through the injected `IInstrumentReferential`; callers do not select a provider asset class or Massive symbol. For example, `SPX` can resolve to Massive's `I:SPX`, while a canonical option or future identifier is mapped explicitly in the referential. A query crossing mapping validity periods is split internally and the returned bars retain the canonical Njord ticker. Missing, ambiguous, or incomplete mappings fail the provider-specific request before network dispatch. The common vector API instead returns a mapping diagnostic for that ticker while continuing the other tickers.

Daily requests are translated to Massive session aggregates for futures while other assets use their daily endpoint, so a daily vector can include both. Explicit session intervals remain futures-only. For futures, date filters apply to `window_start`; session candles can begin on the previous calendar date to settle on the requested trading date. Results are returned in request ticker order and chronological order per ticker. Pagination links are followed only when they remain on the configured API origin.

The provider-specific `Adjusted` option applies to the whole vector. `true` is rejected for a batch containing futures because their aggregate endpoint does not support split adjustment; `false` is allowed and omitted from futures requests. The common provider-neutral API always requests unadjusted prices.

The adapter maps OHLC prices and the available volume, VWAP, transaction count, and adjustment flag. Massive aggregate responses do not consistently provide the quote currency. The models therefore leave `Currency` unset; a future data-enrichment project can populate it. OHLC uses the provider's aggregate timestamp as both `Timestamp` and the indicative `PeriodStart`; `PeriodEnd` is not inferred. Fixed second, minute, hour, and day requests populate `Interval`; calendar and session intervals remain unset.

This project retrieves historical OHLC bars. It does not retrieve standalone spot snapshots, instrument descriptions, or enrich missing currency metadata.

## Common provider and corporate-action contracts

`MassiveHistoricalMarketDataProvider` is registered as `IMarketDataProvider` and
`ICorporateActionProvider` in addition to the Massive-specific OHLC contract.
The common OHLC request accepts canonical tickers and a provider-neutral
interval. It explicitly requests unadjusted bars and returns one result with
diagnostics per ticker. Applications that register multiple providers can
inject `MarketDataProviderSelector` and select a provider by its registered
name, such as `Massive` or `YahooFinance`.

The optional corporate-action capability retrieves stock splits, stock and
cash dividends, ticker changes, and IPOs where Massive's endpoints return
those events. It emits an `UnsupportedActionTypes` diagnostic because mergers
and delistings are not available through these endpoints. Corporate actions
are source-reported details only; no adjusted-price series or adjustment
factors are calculated.

```csharp
var provider = providerSelector.GetProvider("Massive");
var bars = await provider.GetHistoricalOhlcAsync(new MarketDataHistoricalOhlcRequest
{
    Tickers = ["AAPL"],
    From = new DateOnly(2025, 1, 1),
    To = new DateOnly(2025, 1, 31),
    Multiplier = 1,
    Interval = MarketDataInterval.Day
});
```

## Configuration and credentials

The provider binds its options from the `DataProviders:Massive` section supplied through DI; it does not read environment variables. The source-owned `massive.settings.json` contains these settings, and the Massive demo uses it directly without a local overlay. Its `ApiKey` is empty by default. Under the current project policy, keys for free/public services may be stored in this tracked settings file; do not place keys in code or logs. If a provider key later grants paid or otherwise confidential access, move it to a protected local JSON configuration file before using it.

HTTP 429 responses and transient network/server failures use the shared retry policy in `Njord.Core.Retry`. The `DataProviders:Massive:Retry:Pattern` setting selects `Exponential` or `Linear` backoff; delays are capped by `MaximumDelay` and extended when Massive supplies a larger `Retry-After` value. The provider surfaces `MassiveRateLimitException` if the rate limit remains exceeded after the configured attempts. Repeated transient server/network failures open a circuit breaker. Timeout, retry, breaker, and page-count options are validated at application startup.

HTTP 401/403 responses are not retried. The exception includes Massive's structured `error`/`message` detail and request ID when the response provides them; malformed or non-JSON error bodies retain the HTTP status without echoing the raw body. Verify the API key and the account's access to the requested product/endpoint in the Massive dashboard. A syntactically configured key does not establish that it is valid or entitled to the requested data.

Register the referential and provider with the host's `IConfiguration`. Configure the referential's primary JSON database path in its own local configuration section:

```csharp
var massiveConfiguration = new ConfigurationBuilder()
    .AddJsonFile("massive.settings.json", optional: false, reloadOnChange: false)
    .Build();
var referentialConfiguration = new ConfigurationBuilder()
    .AddJsonFile("referential.settings.local.json", optional: false, reloadOnChange: false)
    .Build();

services.AddNjordReferential(referentialConfiguration);
services.AddMassiveDataProvider(massiveConfiguration);
```

Then inject `IMassiveHistoricalMarketDataProvider` and call `GetHistoricalOhlcAsync` with a `MassiveHistoricalOhlcRequest` containing `Tickers`, `From`, `To`, `Multiplier`, and `Timespan`. The JSON database must contain mappings for every ticker, provider, and date in the requested range. Provider ticker formats and available history/recency depend on the Massive account plan.

See [Njord.Data.Referential](../Njord.Data.Referential/README.md) for mapping rules and the primary JSON database format.

## Massive endpoint references

- [Stocks custom bars](https://massive.com/docs/rest/stocks/aggregates/custom-bars.md)
- [Indices custom bars](https://massive.com/docs/rest/indices/aggregates/custom-bars.md)
- [Options custom bars](https://massive.com/docs/rest/options/aggregates/custom-bars.md)
- [Crypto custom bars](https://massive.com/docs/rest/crypto/aggregates/custom-bars.md)
- [Forex custom bars](https://massive.com/docs/rest/forex/aggregates/custom-bars.md)
- [Futures aggregate bars](https://massive.com/docs/rest/futures/aggregates.md)
- [REST authentication](https://massive.com/docs/rest/quickstart.md)
