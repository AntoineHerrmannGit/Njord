# Njord.Models

Provider-neutral business models for financial instruments and timestamped market data.

Create models with object initializers and explicit properties:

```csharp
var stock = new Stock
{
    Ticker = "AAPL",
    Timestamp = DateTimeOffset.UtcNow
};
```

`IFinancialInstrument` exposes the identity shared by ticker-based instruments: `Ticker` and `Timestamp`. `FinancialInstrument` implements that contract for stocks, futures, options, indices, forex, swaps, bonds, and repos. `ITimestampedMarketData` provides the shared observation timestamp for rates, curves, correlations, volatility, forward rates, discount factors, spot prices, and OHLC bars, which are market data rather than ticker-based tradable instruments.

Instrument-specific terms are represented by their respective model: option underlying, expiration, type, and strike; contract dates and available rates/notionals for derivatives; currencies and cash/face values where applicable; and dated points for yield curves. Fields not known or not applicable may be nullable where the model allows it. This project does not price instruments or infer missing terms.

Instrument rates, curve points, coupon values, spot prices, and OHLC prices use `decimal`. Calculated statistical values and curve factors use `double`: correlation is bounded by -1 and 1, volatility is a non-negative decimal fraction, forward rates are decimal fractions, and discount factors are positive dimensionless ratios. Correlation and volatility are market observations identified by their maturity, observation timestamp, and kind; correlations also identify the statistical method used. Volatility records whether it is annualized. Forward rates and discount factors identify their curve and currency. Spot and OHLC records identify the instrument and quote currency. OHLC uses its observation timestamp as the primary time reference; interval and period bounds are optional provider-supplied metadata. These models represent supplied results but do not define calculation conventions or perform calculations. Currency identifiers are strings as supplied by the owning application/provider; the models do not impose a market-specific currency or ticker convention. Timestamps use `DateTimeOffset` to retain their UTC offset.
