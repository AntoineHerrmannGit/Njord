using System.Net;
using System.Text;
using Njord.DataProviders.YahooFinance;
using Njord.MarketData;

namespace Njord.DataProviders.YahooFinance.Tests;

public sealed class YahooFinanceMarketDataProviderTests
{
    [Fact]
    public async Task GetHistoricalBarsAsync_MapsBarsAndPreservesAdjustedClose()
    {
        using var client = CreateClient(HistoricalResponse);
        var provider = new YahooFinanceMarketDataProvider(client);
        var request = new HistoricalMarketDataRequest(
            new MarketInstrument("aapl", MarketAssetClass.Equity),
            DateTimeOffset.FromUnixTimeSeconds(1_735_689_600),
            DateTimeOffset.FromUnixTimeSeconds(1_735_862_400),
            MarketDataInterval.OneDay);

        var bars = await provider.GetHistoricalBarsAsync(request);

        Assert.Equal(2, bars.Count);
        Assert.Equal("AAPL", bars[0].Instrument.Symbol);
        Assert.Equal("NASDAQ", bars[0].Instrument.Exchange);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), bars[0].Timestamp);
        Assert.Equal(10m, bars[0].Open);
        Assert.Equal(12m, bars[0].High);
        Assert.Equal(9m, bars[0].Low);
        Assert.Equal(11m, bars[0].Close);
        Assert.Equal(1_000m, bars[0].Volume);
        Assert.Equal(10.5m, bars[0].AdjustedClose);
        Assert.Null(bars[1].Volume);
        Assert.Equal("USD", bars[0].Currency);
        Assert.Equal(provider.ProviderId, bars[0].Provider);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_UsesInjectedSolutionJsonSerializer()
    {
        var serializer = new CountingJsonSerializer();
        using var client = CreateClient(HistoricalResponse);
        var provider = new YahooFinanceMarketDataProvider(client, serializer);

        await provider.GetHistoricalBarsAsync(CreateRequest(MarketDataInterval.OneDay));

        Assert.Equal(1, serializer.DeserializeCallCount);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_SendsRequestedRangeAndInterval()
    {
        HttpRequestMessage? capturedRequest = null;
        using var client = CreateClient((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(HistoricalResponse));
        });
        var provider = new YahooFinanceMarketDataProvider(client);
        var start = DateTimeOffset.FromUnixTimeSeconds(1_735_689_600);
        var end = DateTimeOffset.FromUnixTimeSeconds(1_735_862_400);
        var query = new HistoricalMarketDataRequest(
            new MarketInstrument("AAPL", MarketAssetClass.Equity),
            start,
            end,
            MarketDataInterval.OneDay);

        await provider.GetHistoricalBarsAsync(query);

        Assert.NotNull(capturedRequest);
        Assert.Contains("/AAPL?", capturedRequest.RequestUri!.PathAndQuery);
        Assert.Contains($"period1={start.ToUnixTimeSeconds()}", capturedRequest.RequestUri.Query);
        Assert.Contains($"period2={end.ToUnixTimeSeconds()}", capturedRequest.RequestUri.Query);
        Assert.Contains("interval=1d", capturedRequest.RequestUri.Query);
    }

    [Theory]
    [InlineData(MarketAssetClass.Index, "^GSPC")]
    [InlineData(MarketAssetClass.Future, "ES=F")]
    [InlineData(MarketAssetClass.ForeignExchange, "EURUSD=X")]
    [InlineData(MarketAssetClass.Option, "AAPL250117C00100000")]
    public async Task GetHistoricalBarsAsync_SupportsAdditionalAssetClasses(
        MarketAssetClass assetClass,
        string symbol)
    {
        using var client = CreateClient(HistoricalResponse);
        var provider = new YahooFinanceMarketDataProvider(client);
        var request = new HistoricalMarketDataRequest(
            new MarketInstrument(symbol, assetClass),
            DateTimeOffset.FromUnixTimeSeconds(1_735_689_600),
            DateTimeOffset.FromUnixTimeSeconds(1_735_862_400),
            MarketDataInterval.OneDay);

        var bars = await provider.GetHistoricalBarsAsync(request);

        Assert.NotEmpty(bars);
        Assert.Equal(assetClass, bars[0].Instrument.AssetClass);
        Assert.Equal(symbol, bars[0].Instrument.Symbol);
    }

    [Fact]
    public async Task GetQuoteAsync_MapsYahooRegularMarketQuote()
    {
        using var client = CreateClient(QuoteResponse);
        var provider = new YahooFinanceMarketDataProvider(client);

        var quote = await provider.GetQuoteAsync(new MarketInstrument("AAPL", MarketAssetClass.Equity));

        Assert.Equal(123.45m, quote.LastPrice);
        Assert.Equal(122m, quote.PreviousClose);
        Assert.Equal(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), quote.Timestamp);
        Assert.Equal("USD", quote.Currency);
        Assert.Equal("NASDAQ", quote.Instrument.Exchange);
        Assert.Null(quote.BidPrice);
        Assert.Null(quote.AskPrice);
    }

    [Fact]
    public async Task GetQuoteAsync_FallsBackToLatestAvailableBar()
    {
        using var client = CreateClient(HistoricalResponse);
        var provider = new YahooFinanceMarketDataProvider(client);

        var quote = await provider.GetQuoteAsync(new MarketInstrument("AAPL", MarketAssetClass.Equity));

        Assert.Equal(12m, quote.LastPrice);
        Assert.Equal(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), quote.Timestamp);
    }

    [Theory]
    [InlineData(MarketAssetClass.Index, "^GSPC")]
    [InlineData(MarketAssetClass.Future, "ES=F")]
    [InlineData(MarketAssetClass.ForeignExchange, "EURUSD=X")]
    [InlineData(MarketAssetClass.Option, "AAPL250117C00100000")]
    public async Task GetQuoteAsync_SupportsAdditionalAssetClasses(
        MarketAssetClass assetClass,
        string symbol)
    {
        using var client = CreateClient(HistoricalResponse);
        var provider = new YahooFinanceMarketDataProvider(client);

        var quote = await provider.GetQuoteAsync(new MarketInstrument(symbol, assetClass));

        Assert.Equal(assetClass, quote.Instrument.AssetClass);
        Assert.Equal(symbol, quote.Instrument.Symbol);
    }

    [Fact]
    public async Task GetOptionChainAsync_MapsCallsPutsAndAvailableExpirations()
    {
        using var client = CreateClient(OptionChainHandler);
        var provider = new YahooFinanceMarketDataProvider(client);

        var chain = await provider.GetOptionChainAsync(
            new OptionChainRequest(new MarketInstrument("AAPL", MarketAssetClass.Equity)));

        Assert.Equal(new DateOnly(2025, 1, 17), chain.SelectedExpiration);
        Assert.Equal(new[] { new DateOnly(2025, 1, 17), new DateOnly(2025, 1, 24) }, chain.AvailableExpirations);
        Assert.Equal(2, chain.Contracts.Count);
        var call = Assert.Single(chain.Contracts, contract => contract.Type == OptionType.Call);
        Assert.Equal("AAPL250117C00100000", call.Instrument.Symbol);
        Assert.Equal("AAPL", call.Underlying.Symbol);
        Assert.Equal(100m, call.Strike);
        Assert.Equal(3.5m, call.LastPrice);
        Assert.Equal(3.4m, call.BidPrice);
        Assert.Equal(3.6m, call.AskPrice);
        Assert.Equal(1234L, call.Volume);
        Assert.Equal(5000L, call.OpenInterest);
        Assert.Equal(0.25m, call.ImpliedVolatility);
        Assert.True(call.IsInTheMoney);
        Assert.Equal("USD", call.Currency);
        Assert.Equal("YahooFinance", chain.Provider);
        var put = Assert.Single(chain.Contracts, contract => contract.Type == OptionType.Put);
        Assert.Equal("AAPL250117P00100000", put.Instrument.Symbol);
        Assert.False(put.IsInTheMoney);
    }

    [Fact]
    public async Task GetOptionChainAsync_RequestsTheSelectedExpirationAndBootstrapsYahooSession()
    {
        var requests = new List<string>();
        using var client = CreateClient((request, _) =>
        {
            requests.Add(request.RequestUri!.ToString());
            if (request.RequestUri.Host == "fc.yahoo.com")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            if (request.RequestUri.AbsolutePath == "/v1/test/getcrumb")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("test-crumb")
                });
            }

            return Task.FromResult(JsonResponse(OptionChainResponse));
        });
        var provider = new YahooFinanceMarketDataProvider(client);

        await provider.GetOptionChainAsync(
            new OptionChainRequest(
                new MarketInstrument("AAPL", MarketAssetClass.Equity),
                new DateOnly(2025, 1, 17)));

        Assert.Contains(requests, uri => uri.StartsWith("https://fc.yahoo.com/", StringComparison.Ordinal));
        Assert.Contains(requests, uri => uri.StartsWith(
            "https://query1.finance.yahoo.com/v1/test/getcrumb",
            StringComparison.Ordinal));
        Assert.Contains(requests, uri =>
            uri.Contains("/v7/finance/options/AAPL?", StringComparison.Ordinal) &&
            uri.Contains("date=1737072000", StringComparison.Ordinal) &&
            uri.Contains("crumb=test-crumb", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetOptionChainAsync_ReportsYahooSessionAuthorizationErrors()
    {
        using var client = CreateClient((request, _) =>
        {
            if (request.RequestUri!.Host == "fc.yahoo.com")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            if (request.RequestUri.AbsolutePath == "/v1/test/getcrumb")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("test-crumb")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        var provider = new YahooFinanceMarketDataProvider(client);

        var exception = await Assert.ThrowsAsync<MarketDataProviderException>(
            () => provider.GetOptionChainAsync(
                new OptionChainRequest(new MarketInstrument("AAPL", MarketAssetClass.Equity))));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("HTTP 401", exception.Message);
    }

    [Fact]
    public async Task GetOptionChainAsync_RefreshesExpiredYahooCrumbOnce()
    {
        var optionsRequests = 0;
        var crumbRequests = 0;
        using var client = CreateClient((request, _) =>
        {
            if (request.RequestUri!.Host == "fc.yahoo.com")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            if (request.RequestUri.AbsolutePath == "/v1/test/getcrumb")
            {
                crumbRequests++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"test-crumb-{crumbRequests}")
                });
            }

            optionsRequests++;
            return Task.FromResult(optionsRequests == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : JsonResponse(OptionChainResponse));
        });
        var provider = new YahooFinanceMarketDataProvider(client);

        var chain = await provider.GetOptionChainAsync(
            new OptionChainRequest(new MarketInstrument("AAPL", MarketAssetClass.Equity)));

        Assert.Equal(new DateOnly(2025, 1, 17), chain.SelectedExpiration);
        Assert.Equal(2, optionsRequests);
        Assert.Equal(2, crumbRequests);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_ReportsYahooApiErrors()
    {
        using var client = CreateClient(
            """{"chart":{"result":null,"error":{"code":"Not Found","description":"No data found"}}}""");
        var provider = new YahooFinanceMarketDataProvider(client);
        var request = CreateRequest(MarketDataInterval.OneDay);

        var exception = await Assert.ThrowsAsync<MarketDataProviderException>(
            () => provider.GetHistoricalBarsAsync(request));

        Assert.Equal(provider.ProviderId, exception.ProviderId);
        Assert.Contains("Not Found", exception.Message);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_ExposesHttpRateLimitStatus()
    {
        using var client = CreateClient((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var provider = new YahooFinanceMarketDataProvider(client);

        var exception = await Assert.ThrowsAsync<MarketDataProviderException>(
            () => provider.GetHistoricalBarsAsync(CreateRequest(MarketDataInterval.OneDay)));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_RejectsMalformedSeries()
    {
        const string response =
            """{"chart":{"result":[{"meta":{"currency":"USD"},"timestamp":[1735689600],"indicators":{"quote":[{"open":[],"high":[12],"low":[9],"close":[11]}]}}],"error":null}}""";
        using var client = CreateClient(response);
        var provider = new YahooFinanceMarketDataProvider(client);

        var exception = await Assert.ThrowsAsync<MarketDataProviderException>(
            () => provider.GetHistoricalBarsAsync(CreateRequest(MarketDataInterval.OneDay)));

        Assert.Contains("open series", exception.Message);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_ReportsUnexpectedJsonShape()
    {
        using var client = CreateClient("""[]""");
        var provider = new YahooFinanceMarketDataProvider(client);

        var exception = await Assert.ThrowsAsync<MarketDataProviderException>(
            () => provider.GetHistoricalBarsAsync(CreateRequest(MarketDataInterval.OneDay)));

        Assert.Contains("malformed JSON", exception.Message);
    }

    [Fact]
    public void YahooFinanceResponseDtos_AreNotPublicApi()
    {
        var assembly = typeof(YahooFinanceMarketDataProvider).Assembly;

        Assert.Null(assembly.GetExportedTypes().SingleOrDefault(
            type => type.Name.Contains("Response", StringComparison.Ordinal) ||
                    type.Name.Contains("Dto", StringComparison.OrdinalIgnoreCase)));
        Assert.False(assembly.GetType(
            "Njord.DataProviders.YahooFinance.Serialization.YahooFinanceChartResponse",
            throwOnError: true)!.IsPublic);
        Assert.False(assembly.GetType(
            "Njord.DataProviders.YahooFinance.Serialization.YahooFinanceOptionChainResponse",
            throwOnError: true)!.IsPublic);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_RejectsOutOfRangeTimestamp()
    {
        const string response =
            """{"chart":{"result":[{"meta":{"currency":"USD"},"timestamp":[999999999999999],"indicators":{"quote":[{"open":[10],"high":[12],"low":[9],"close":[11]}]}}],"error":null}}""";
        using var client = CreateClient(response);
        var provider = new YahooFinanceMarketDataProvider(client);

        var exception = await Assert.ThrowsAsync<MarketDataProviderException>(
            () => provider.GetHistoricalBarsAsync(CreateRequest(MarketDataInterval.OneDay)));

        Assert.Contains("'timestamp'", exception.Message);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_RejectsUnsupportedAssetClassWithoutRequest()
    {
        var requestSent = false;
        using var client = CreateClient((_, _) =>
        {
            requestSent = true;
            return Task.FromResult(JsonResponse(HistoricalResponse));
        });
        var provider = new YahooFinanceMarketDataProvider(client);
        var request = new HistoricalMarketDataRequest(
            new MarketInstrument("SOFR", MarketAssetClass.InterestRate),
            DateTimeOffset.FromUnixTimeSeconds(1_735_689_600),
            DateTimeOffset.FromUnixTimeSeconds(1_735_862_400),
            MarketDataInterval.OneDay);

        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetHistoricalBarsAsync(request));

        Assert.False(requestSent);
    }

    [Fact]
    public async Task GetHistoricalBarsAsync_PropagatesCancellation()
    {
        using var client = CreateClient((_, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(JsonResponse(HistoricalResponse));
        });
        var provider = new YahooFinanceMarketDataProvider(client);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.GetHistoricalBarsAsync(CreateRequest(MarketDataInterval.OneDay), cancellationSource.Token));
    }

    [Fact]
    public void MarketDataBar_RejectsNegativeVolume()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarketDataBar(
            new MarketInstrument("AAPL", MarketAssetClass.Equity),
            DateTimeOffset.UnixEpoch,
            MarketDataInterval.OneDay,
            10m,
            12m,
            9m,
            11m,
            -1m,
            "USD",
            "Test"));
    }

    private static HistoricalMarketDataRequest CreateRequest(MarketDataInterval interval) =>
        new(
            new MarketInstrument("AAPL", MarketAssetClass.Equity),
            DateTimeOffset.FromUnixTimeSeconds(1_735_689_600),
            DateTimeOffset.FromUnixTimeSeconds(1_735_862_400),
            interval);

    private static HttpClient CreateClient(string response) =>
        CreateClient((_, _) => Task.FromResult(JsonResponse(response)));

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new StubHttpMessageHandler(handler));

    private static HttpResponseMessage JsonResponse(string response) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(response, Encoding.UTF8, "application/json")
        };

    private static Task<HttpResponseMessage> OptionChainHandler(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.RequestUri!.Host == "fc.yahoo.com")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        if (request.RequestUri.AbsolutePath == "/v1/test/getcrumb")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("test-crumb")
            });
        }

        return Task.FromResult(JsonResponse(OptionChainResponse));
    }

    private const string HistoricalResponse =
        """
        {
          "chart": {
            "result": [{
              "meta": {
                "currency": "USD",
                "exchangeName": "NMS",
                "fullExchangeName": "NASDAQ"
              },
              "timestamp": [1735689600, 1735776000],
              "indicators": {
                "quote": [{
                  "open": [10, 11],
                  "high": [12, 13],
                  "low": [9, 10],
                  "close": [11, 12],
                  "volume": [1000, null]
                }],
                "adjclose": [{
                  "adjclose": [10.5, 11.5]
                }]
              }
            }],
            "error": null
          }
        }
        """;

    private const string QuoteResponse =
        """
        {
          "chart": {
            "result": [{
              "meta": {
                "currency": "USD",
                "fullExchangeName": "NASDAQ",
                "regularMarketPrice": 123.45,
                "regularMarketTime": 1735776000,
                "chartPreviousClose": 122
              },
              "timestamp": [],
              "indicators": { "quote": [] }
            }],
            "error": null
          }
        }
        """;

    private const string OptionChainResponse =
        """
        {
          "optionChain": {
            "result": [{
              "underlyingSymbol": "AAPL",
              "expirationDates": [1737072000, 1737676800],
              "quote": {
                "currency": "USD",
                "fullExchangeName": "NASDAQ"
              },
              "options": [{
                "expirationDate": 1737072000,
                "calls": [{
                  "contractSymbol": "AAPL250117C00100000",
                  "strike": 100,
                  "currency": "USD",
                  "lastPrice": 3.5,
                  "bid": 3.4,
                  "ask": 3.6,
                  "lastTradeDate": 1736899200,
                  "volume": 1234,
                  "openInterest": 5000,
                  "impliedVolatility": 0.25,
                  "inTheMoney": true,
                  "expiration": 1737072000
                }],
                "puts": [{
                  "contractSymbol": "AAPL250117P00100000",
                  "strike": 100,
                  "currency": "USD",
                  "lastPrice": 2.5,
                  "bid": 2.4,
                  "ask": 2.6,
                  "expiration": 1737072000,
                  "inTheMoney": false
                }]
              }]
            }],
            "error": null
          }
        }
        """;

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request, cancellationToken);
    }

    private sealed class CountingJsonSerializer : Njord.Core.Serialization.IJsonSerializer
    {
        private readonly Njord.Core.Serialization.SystemTextJsonSerializer _inner = new();

        public int DeserializeCallCount { get; private set; }

        public string Serialize<T>(T value) => _inner.Serialize(value);

        public T Deserialize<T>(string json) => _inner.Deserialize<T>(json);

        public Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default) =>
            _inner.SerializeAsync(stream, value, cancellationToken);

        public Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            DeserializeCallCount++;
            return _inner.DeserializeAsync<T>(stream, cancellationToken);
        }
    }
}
