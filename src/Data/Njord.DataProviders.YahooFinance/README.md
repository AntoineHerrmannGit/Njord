# Yahoo Finance provider

`YahooFinanceMarketDataProvider` implements `IMarketDataProvider` using Yahoo Finance's chart and options endpoints. It returns the provider-neutral models from `Njord.MarketData`.
The public surface is limited to this provider and the market-data business contracts/models in `Njord.MarketData`; Yahoo response envelopes and JSON readers are internal implementation details.
It uses `IJsonSerializer` from `Njord.Core.Serialization`; applications can pass their shared serializer through the provider's two-argument constructor.

## Code organization

- `Transport/` owns HTTP, Yahoo session cookies, crumb acquisition, and request/response boundaries.
- `Serialization/` contains internal Yahoo response envelopes and JSON validation helpers.
- `Chart/` maps chart responses to common OHLCV bars and quotes.
- `Options/` maps option-chain responses to common option-chain business models.

```csharp
using Njord.DataProviders.YahooFinance;
using Njord.MarketData;

using var httpClient = new HttpClient();
var provider = new YahooFinanceMarketDataProvider(httpClient);

var request = new HistoricalMarketDataRequest(
    new MarketInstrument("^GSPC", MarketAssetClass.Index),
    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
    new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
    MarketDataInterval.OneDay);

var bars = await provider.GetHistoricalBarsAsync(request, cancellationToken);
var latestQuote = await provider.GetQuoteAsync(request.Instrument, cancellationToken);

var optionChain = await provider.GetOptionChainAsync(
    new OptionChainRequest(
        new MarketInstrument("AAPL", MarketAssetClass.Equity),
        new DateOnly(2025, 1, 17)),
    cancellationToken);
```

## Supported instruments and symbols

Historical OHLCV bars and latest quotes support equities, indices, futures, FX pairs, and individual option contracts through Yahoo's chart endpoint. Pass Yahoo Finance's symbol form in `MarketInstrument.Symbol`: for example `AAPL`, `^GSPC`, `ES=F`, `EURUSD=X`, or an option contract symbol such as `AAPL250117C00100000`. The provider trims and uppercases symbols before sending requests and in returned instrument identifiers. It does not infer, convert, or validate provider-specific symbol conventions.

Option chains are requested separately for an equity or index underlying. Specify an expiration date to select that chain; if omitted, Yahoo's default expiration is returned. The response also includes all expiration dates Yahoo reported. Chain contracts include calls and puts, strike, expiration, currency, last trade, bid/ask, volume, open interest, implied volatility, and in-the-money status when Yahoo supplies those fields. Implied volatility is represented as a decimal fraction (for example, `0.25` means 25%). To request bars for an individual option, use its returned contract symbol in a `MarketInstrument` with `MarketAssetClass.Option`.

Yahoo may omit volume, adjusted close, bid/ask, or option quote fields; those remain `null`. Historical ranges use an inclusive start and exclusive end. Returned timestamps are UTC instants. Bar OHLC values are unadjusted; `AdjustedClose` remains separate and is populated when Yahoo supplies it. Options chains contain one selected expiration per call; they are not a consolidated chain snapshot across every expiration.

## Operational limits

These are undocumented Yahoo Finance endpoints and their availability, fields, interval limits, and rate limits may change without notice. Option-chain access bootstraps a Yahoo session cookie and crumb; the injected `HttpClient` must use a cookie-enabled HTTP handler (the standard .NET handler does). If Yahoo rejects a chain request with `401 Unauthorized`, the provider refreshes the crumb and retries that request once. Other HTTP failures, including `429 Too Many Requests`, and invalid API data are surfaced as `MarketDataProviderException` without automatic retry. The application owns and configures the injected `HttpClient`, including its timeout and any rate-limit policy. Cancellation is propagated to network and response-reading operations. The provider does not guarantee quote freshness, option-chain completeness, or market-session coverage.
