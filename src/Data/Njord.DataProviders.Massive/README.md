# Njord.DataProviders.Massive

Historical OHLC adapter for Massive. Vendor response DTOs, HTTP transport, rate limiting, resilience, and mapping remain inside this project; callers receive provider-neutral `Njord.Models.MarketData.Ohlc` records.

## Supported historical aggregates

The provider currently retrieves aggregate bars for stocks, indices, options, crypto, forex, and futures. The first five asset classes use Massive's `/v2/aggs/ticker/.../range/...` endpoint; futures use `/futures/v1/aggs/{ticker}` and its `resolution`/`window_start` query format. Futures timestamps are nanoseconds; other aggregate timestamps are milliseconds. All timestamps are converted to UTC `DateTimeOffset`.

The request ticker must use Massive's symbology without provider-side rewriting (for example `AAPL`, `I:SPX`, `O:...`, `X:BTCUSD`, `C:EURUSD`, or `ESU5`). For futures, date filters apply to `window_start`; session candles can begin on the previous calendar date to settle on the requested trading date. Results follow the provider's ascending order and pagination links are followed only when they remain on the configured API origin.

The adapter maps OHLC prices and the available volume, VWAP, transaction count, and adjustment flag. Massive aggregate responses do not consistently provide the quote currency. The models therefore leave `Currency` unset; a future data-enrichment project can populate it. OHLC uses the provider's aggregate timestamp as both `Timestamp` and the indicative `PeriodStart`; `PeriodEnd` is not inferred. Fixed second, minute, hour, and day requests populate `Interval`; calendar and session intervals remain unset.

This project retrieves historical OHLC bars. It does not retrieve standalone spot snapshots, instrument descriptions, or enrich missing currency metadata.

## Configuration and credentials

Copy `massive.settings.example.json` to the host's `massive.settings.local.json` and set `DataProviders:Massive:ApiKey` in that local file. The provider binds all its options, including the API key, from the `DataProviders:Massive` section supplied through DI; it does not read environment variables. The local file is excluded from Git. Restrict its filesystem permissions so only the application user can read it, and never paste the key into source, logs, or chat. For CI/CD, provide the JSON configuration as a protected secret file and make it available to the application at runtime; do not commit it or pass the credential through environment variables.

`RequestsPerMinute` is configurable per account plan (the sample uses five). Each request attempt, including retries and pagination, passes through a shared provider rate limiter. HTTP 429 responses surface as `MassiveRateLimitException` with the server's `Retry-After` value when supplied. Transient HTTP failures use bounded exponential retries; repeated transient failures open a circuit breaker. Timeout, retry, breaker, and page-count options are validated at application startup.

Register the provider with the host's `IConfiguration`:

```csharp
builder.Configuration.AddJsonFile(
    "massive.settings.local.json",
    optional: false,
    reloadOnChange: false);

services.AddMassiveDataProvider(builder.Configuration);
```

Then inject `IMassiveHistoricalMarketDataProvider` and call `GetHistoricalOhlcAsync` with a `MassiveHistoricalOhlcRequest`. Provider ticker formats and available history/recency depend on the Massive account plan.

## Massive endpoint references

- [Stocks custom bars](https://massive.com/docs/rest/stocks/aggregates/custom-bars.md)
- [Indices custom bars](https://massive.com/docs/rest/indices/aggregates/custom-bars.md)
- [Options custom bars](https://massive.com/docs/rest/options/aggregates/custom-bars.md)
- [Crypto custom bars](https://massive.com/docs/rest/crypto/aggregates/custom-bars.md)
- [Forex custom bars](https://massive.com/docs/rest/forex/aggregates/custom-bars.md)
- [Futures aggregate bars](https://massive.com/docs/rest/futures/aggregates.md)
- [REST authentication](https://massive.com/docs/rest/quickstart.md)
