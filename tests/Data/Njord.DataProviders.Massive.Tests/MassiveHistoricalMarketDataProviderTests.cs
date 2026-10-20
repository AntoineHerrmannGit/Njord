using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Njord.Core.Retry;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;
using Njord.DataProviders.Massive.Mapping;
using Njord.DataProviders.Massive.Provider;
using Njord.DataProviders.Massive.Transport;
using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Tests;

public sealed class MassiveHistoricalMarketDataProviderTests
{
    #region Asset class endpoints

    [Theory]
    [InlineData("Stock", "AAPL", "AAPL", "/v2/aggs/ticker/AAPL/range/1/day/2025-01-01/2025-01-31")]
    [InlineData("Index", "SPX", "I:SPX", "/v2/aggs/ticker/I:SPX/range/1/day/2025-01-01/2025-01-31")]
    [InlineData("Option", "AAPL-2025-01-17-C-100", "O:AAPL250117C00100000", "/v2/aggs/ticker/O:AAPL250117C00100000/range/1/day/2025-01-01/2025-01-31")]
    [InlineData("Crypto", "BTC/USD", "X:BTCUSD", "/v2/aggs/ticker/X:BTCUSD/range/1/day/2025-01-01/2025-01-31")]
    [InlineData("Forex", "EUR/USD", "C:EURUSD", "/v2/aggs/ticker/C:EURUSD/range/1/day/2025-01-01/2025-01-31")]
    [InlineData("Future", "ES=2025-01-17", "ESU5", "/futures/v1/aggs/ESU5")]
    public async Task GetHistoricalOhlcAsync_UsesAssetSpecificEndpointAndMapsMarketData(
        string assetClassName,
        string njordTicker,
        string providerTicker,
        string expectedPath)
    {
        var assetClass = Enum.Parse<MassiveAssetClass>(assetClassName);
        var response = assetClass == MassiveAssetClass.Future ? FuturesResponse : StandardResponse;
        var handler = new QueueHttpMessageHandler(JsonResponse(response));
        var provider = CreateProvider(handler);
        var request = CreateRequest(assetClass, njordTicker);

        var results = await provider.GetHistoricalOhlcAsync(request);

        var sentRequest = Assert.Single(handler.Requests);
        var requestUri = sentRequest.Uri!;
        Assert.Equal(expectedPath, Uri.UnescapeDataString(requestUri.AbsolutePath));
        Assert.Contains(providerTicker, Uri.UnescapeDataString(requestUri.AbsolutePath), StringComparison.Ordinal);
        Assert.Equal("Bearer", sentRequest.Authorization?.Scheme);
        Assert.Equal("test-api-key", sentRequest.Authorization?.Parameter);
        Assert.DoesNotContain("apiKey", requestUri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.Single(results);
        Assert.Equal(njordTicker, results[0].Ticker);
        Assert.Equal(10.25m, results[0].Open);
        Assert.Equal(12m, results[0].High);
        Assert.Equal(9.5m, results[0].Low);
        Assert.Equal(11.75m, results[0].Close);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1735732800000), results[0].Timestamp);
        Assert.Equal(results[0].Timestamp, results[0].PeriodStart);
        Assert.Null(results[0].PeriodEnd);
        Assert.Null(results[0].Currency);
        Assert.Equal(1500m, results[0].Volume);
        Assert.Equal(
            assetClass == MassiveAssetClass.Future ? null : 10.9m,
            results[0].VolumeWeightedAveragePrice);
        Assert.Equal(72, results[0].TransactionCount);
        Assert.Equal(assetClass == MassiveAssetClass.Future ? null : true, results[0].IsAdjusted);

        if (assetClass == MassiveAssetClass.Future)
        {
            Assert.Contains("resolution=1min", requestUri.Query, StringComparison.Ordinal);
            Assert.Contains("window_start.gte=2025-01-01", requestUri.Query, StringComparison.Ordinal);
            Assert.Equal(TimeSpan.FromMinutes(1), results[0].Interval);
        }
        else
            Assert.Contains("/range/1/day/", Uri.UnescapeDataString(requestUri.AbsolutePath), StringComparison.Ordinal);
    }

    #endregion

    #region Pagination and retries

    [Fact]
    public async Task GetHistoricalOhlcAsync_FollowsSameOriginNextUrlWithoutForwardingQueryCredentials()
    {
        var firstPage = """
            {
              "status": "OK",
              "request_id": "first",
              "next_url": "https://api.massive.test/v2/aggs/ticker/AAPL?cursor=next&apiKey=untrusted",
              "results": [
                { "o": 10, "h": 11, "l": 9, "c": 10.5, "t": 1735732800000 }
              ]
            }
            """;
        var secondPage = """
            {
              "status": "OK",
              "request_id": "second",
              "results": [
                { "o": 10.5, "h": 12, "l": 10, "c": 11.5, "t": 1735819200000 }
              ]
            }
            """;
        var handler = new QueueHttpMessageHandler(JsonResponse(firstPage), JsonResponse(secondPage));
        var provider = CreateProvider(handler);

        var bars = await provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL"));

        Assert.Equal(2, bars.Count);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("cursor=next", handler.Requests[1].Uri!.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey", handler.Requests[1].Uri!.Query, StringComparison.OrdinalIgnoreCase);
        Assert.All(handler.Requests, request => Assert.Equal("api.massive.test", request.Uri!.Host));
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_QueriesMultipleCanonicalTickersWithoutCallerAssetClass()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(StandardResponse),
            JsonResponse(StandardResponse),
            JsonResponse(StandardResponse));
        var provider = CreateProvider(handler);
        var request = new MassiveHistoricalOhlcRequest
        {
            Tickers = ["AAPL", "SPX", "EUR/USD"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = MassiveTimespan.Day
        };

        var bars = await provider.GetHistoricalOhlcAsync(request);

        Assert.Equal(["AAPL", "SPX", "EUR/USD"], bars.Select(bar => bar.Ticker));
        Assert.Equal(
            [
                "/v2/aggs/ticker/AAPL/range/1/day/2025-01-01/2025-01-31",
                "/v2/aggs/ticker/I:SPX/range/1/day/2025-01-01/2025-01-31",
                "/v2/aggs/ticker/C:EURUSD/range/1/day/2025-01-01/2025-01-31"
            ],
            handler.Requests.Select(item => Uri.UnescapeDataString(item.Uri!.AbsolutePath)));
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsUnknownCanonicalTickerBeforeCallingMassive()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(handler);
        var request = CreateRequest(MassiveAssetClass.Stock, "UNKNOWN");

        await Assert.ThrowsAsync<TickerReferenceResolutionException>(
            () => provider.GetHistoricalOhlcAsync(request));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsNjordAssetClassNotSupportedByMassive()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(
            handler,
            mappings:
            [
                CreateMapping(
                    "BOND",
                    "US0000000000",
                    NjordAssetClass.Bond)
            ]);

        await Assert.ThrowsAsync<MassiveDataProviderException>(() =>
            provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "BOND")));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsDuplicateTickersBeforeResolving()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(handler);
        var request = new MassiveHistoricalOhlcRequest
        {
            Tickers = ["AAPL", "aapl"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = MassiveTimespan.Day
        };

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetHistoricalOhlcAsync(request));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_SplitsQueryAcrossMappingValidityPeriods()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(StandardResponse),
            JsonResponse(StandardResponse));
        var mappings = new[]
        {
            CreateMapping("AAPL", "AAPL1", NjordAssetClass.Stock, new DateTime(2025, 1, 1), new DateTime(2025, 1, 15)),
            CreateMapping("AAPL", "AAPL2", NjordAssetClass.Stock, new DateTime(2025, 1, 16), new DateTime(2025, 1, 31))
        };
        var provider = CreateProvider(handler, mappings: mappings);
        var request = CreateRequest(MassiveAssetClass.Stock, "AAPL");

        var bars = await provider.GetHistoricalOhlcAsync(request);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(2, bars.Count);
        Assert.All(bars, bar => Assert.Equal("AAPL", bar.Ticker));
        Assert.Contains("/ticker/AAPL1/range/1/day/2025-01-01/2025-01-15", handler.Requests[0].Uri!.AbsolutePath);
        Assert.Contains("/ticker/AAPL2/range/1/day/2025-01-16/2025-01-31", handler.Requests[1].Uri!.AbsolutePath);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RetriesTransientServerFailure()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            JsonResponse(StandardResponse));
        var provider = CreateProvider(handler, retryBaseDelay: TimeSpan.FromTicks(1));

        var bars = await provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL"));

        Assert.Single(bars);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RetriesRateLimitUsingServerRetryAfter()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1));
        var handler = new QueueHttpMessageHandler(response, JsonResponse(StandardResponse));
        var provider = CreateProvider(handler);

        var bars = await provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL"));

        Assert.Single(bars);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_ReportsRateLimitAfterRetryAttemptsAreExhausted()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
        var handler = new QueueHttpMessageHandler(response);
        var provider = CreateProvider(handler, retryAttempts: 0);

        var exception = await Assert.ThrowsAsync<MassiveRateLimitException>(
            () => provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL")));

        Assert.Equal(TimeSpan.FromSeconds(10), exception.RetryAfter);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_OpensCircuitAfterConfiguredTransientFailures()
    {
        var handler = new QueueHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var provider = CreateProvider(
            handler,
            options => options.CircuitBreakerFailureThreshold = 1,
            retryAttempts: 0);
        var request = CreateRequest(MassiveAssetClass.Stock, "AAPL");

        await Assert.ThrowsAsync<MassiveDataProviderException>(() => provider.GetHistoricalOhlcAsync(request));
        await Assert.ThrowsAsync<MassiveCircuitOpenException>(() => provider.GetHistoricalOhlcAsync(request));

        Assert.Single(handler.Requests);
    }

    #endregion

    #region Request and response validation

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsInvalidRequestBeforeSending()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(handler);
        var request = CreateRequest(MassiveAssetClass.Stock, "AAPL") with
        {
            From = new DateOnly(2025, 2, 1),
            To = new DateOnly(2025, 1, 1)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetHistoricalOhlcAsync(request));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsAdjustmentForFutures()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(handler);
        var request = CreateRequest(MassiveAssetClass.Future, "ES=2025-01-17") with { Adjusted = true };

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetHistoricalOhlcAsync(request));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_UsesSessionBarsForFutureInDailyVectorRequest()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(StandardResponse),
            JsonResponse(FuturesResponse));
        var provider = CreateProvider(handler);
        var request = new MassiveHistoricalOhlcRequest
        {
            Tickers = ["AAPL", "ES=2025-01-17"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = MassiveTimespan.Day
        };

        var bars = await provider.GetHistoricalOhlcAsync(request);

        Assert.Equal(["AAPL", "ES=2025-01-17"], bars.Select(bar => bar.Ticker));
        Assert.Contains("/range/1/day/", handler.Requests[0].Uri!.AbsolutePath);
        Assert.Contains("resolution=1session", handler.Requests[1].Uri!.Query, StringComparison.Ordinal);
        Assert.Null(bars[1].Interval);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsInconsistentPriceBars()
    {
        var invalidResponse = """
            {
              "status": "OK",
              "results": [
                { "o": 10, "h": 8, "l": 9, "c": 10, "t": 1735732800000 }
              ]
            }
            """;
        var provider = CreateProvider(new QueueHttpMessageHandler(JsonResponse(invalidResponse)));

        await Assert.ThrowsAsync<MassiveDataProviderException>(
            () => provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL")));
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsResponseForDifferentTicker()
    {
        var response = """
            {
              "status": "OK",
              "ticker": "MSFT",
              "results": []
            }
            """;
        var provider = CreateProvider(new QueueHttpMessageHandler(JsonResponse(response)));

        await Assert.ThrowsAsync<MassiveDataProviderException>(
            () => provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL")));
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsMalformedJson()
    {
        var provider = CreateProvider(new QueueHttpMessageHandler(
            JsonResponse("{\"status\":\"OK\",\"results\":[{\"o\":")));

        await Assert.ThrowsAsync<MassiveDataProviderException>(
            () => provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL")));
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsPaginationOutsideConfiguredOrigin()
    {
        var response = """
            {
              "status": "OK",
              "next_url": "https://attacker.test/steal",
              "results": []
            }
            """;
        var handler = new QueueHttpMessageHandler(JsonResponse(response));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<MassiveDataProviderException>(
            () => provider.GetHistoricalOhlcAsync(CreateRequest(MassiveAssetClass.Stock, "AAPL")));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public void OptionsValidator_ReportsMissingSecretAndInvalidRateLimit()
    {
        var options = new MassiveDataProviderOptions
        {
            BaseAddress = new Uri("https://api.massive.test/"),
            RequestTimeout = TimeSpan.Zero
        };

        var result = new MassiveDataProviderOptionsValidator().Validate(
            Options.DefaultName,
            options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("DataProviders:Massive:ApiKey", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("RequestTimeout", StringComparison.Ordinal));
    }

    [Fact]
    public void AddMassiveDataProvider_BindsApiKeyFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProviders:Massive:BaseAddress"] = "https://api.massive.test/",
                ["DataProviders:Massive:ApiKey"] = "configured-test-key",
                ["DataProviders:Massive:Retry:Pattern"] = "Linear",
                ["DataProviders:Massive:Retry:MaxRetryAttempts"] = "3",
                ["DataProviders:Massive:Retry:BaseDelay"] = "00:00:00.250",
                ["DataProviders:Massive:Retry:MaximumDelay"] = "00:00:05"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddMassiveDataProvider(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MassiveDataProviderOptions>>().Value;
        var retryOptions = serviceProvider.GetRequiredService<IOptions<RetryPolicyOptions>>().Value;

        Assert.Equal("configured-test-key", options.ApiKey);
        Assert.Equal(RetryDelayPattern.Linear, retryOptions.Pattern);
        Assert.Equal(3, retryOptions.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(250), retryOptions.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(5), retryOptions.MaximumDelay);
        Assert.NotNull(serviceProvider.GetRequiredService<RetryPolicy>());
    }

    #endregion

    #region Test helpers

    private const string StandardResponse = """
        {
          "status": "OK",
          "adjusted": true,
          "request_id": "massive-request",
          "results": [
            {
              "o": 10.25,
              "h": 12.0,
              "l": 9.5,
              "c": 11.75,
              "t": 1735732800000,
              "v": 1500,
              "vw": 10.9,
              "n": 72
            }
          ]
        }
        """;

    private const string FuturesResponse = """
        {
          "status": "OK",
          "request_id": "massive-futures-request",
          "results": [
            {
              "open": 10.25,
              "high": 12.0,
              "low": 9.5,
              "close": 11.75,
              "window_start": 1735732800000000000,
              "volume": 1500,
              "transactions": 72
            }
          ]
        }
        """;

    private static MassiveHistoricalMarketDataProvider CreateProvider(
        QueueHttpMessageHandler handler,
        Action<MassiveDataProviderOptions>? configure = null,
        int retryAttempts = 1,
        TimeSpan? retryBaseDelay = null,
        IReadOnlyList<TickerProviderMapping>? mappings = null)
    {
        var options = new MassiveDataProviderOptions
        {
            BaseAddress = new Uri("https://api.massive.test/"),
            ApiKey = "test-api-key",
            RequestTimeout = TimeSpan.FromSeconds(5),
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(1),
            MaximumPagesPerQuery = 10
        };
        configure?.Invoke(options);
        var retryOptions = new RetryPolicyOptions
        {
            MaxRetryAttempts = retryAttempts,
            BaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(1),
            MaximumDelay = TimeSpan.FromSeconds(1)
        };

        var providerOptions = Options.Create(options);
        var timeProvider = TimeProvider.System;
        var apiClient = new MassiveAggregatesApiClient(
            new StubHttpClientFactory(handler),
            providerOptions,
            new RetryPolicy(Options.Create(retryOptions), timeProvider),
            new MassiveCircuitBreaker(providerOptions, timeProvider),
            timeProvider);
        return new MassiveHistoricalMarketDataProvider(
            apiClient,
            new MassiveOhlcMapper(),
            new StubInstrumentReferential(mappings ?? CreateDefaultMappings()));
    }

    private static IReadOnlyList<TickerProviderMapping> CreateDefaultMappings() =>
    [
        CreateMapping("AAPL", "AAPL", NjordAssetClass.Stock),
        CreateMapping("SPX", "I:SPX", NjordAssetClass.Index),
        CreateMapping("AAPL-2025-01-17-C-100", "O:AAPL250117C00100000", NjordAssetClass.Option),
        CreateMapping("BTC/USD", "X:BTCUSD", NjordAssetClass.Crypto),
        CreateMapping("EUR/USD", "C:EURUSD", NjordAssetClass.Forex),
        CreateMapping("ES=2025-01-17", "ESU5", NjordAssetClass.Future)
    ];

    private static TickerProviderMapping CreateMapping(
        string ticker,
        string providerTicker,
        NjordAssetClass assetClass,
        DateTime? startDate = null,
        DateTime? endDate = null) =>
        new()
        {
            Ticker = ticker,
            Provider = "Massive",
            ProviderTicker = providerTicker,
            AssetClass = assetClass,
            StartDate = startDate ?? DateTime.MinValue,
            EndDate = endDate ?? DateTime.MaxValue
        };

    private static MassiveHistoricalOhlcRequest CreateRequest(MassiveAssetClass assetClass, string ticker) =>
        new()
        {
            Tickers = [ticker],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = assetClass == MassiveAssetClass.Future
                ? MassiveTimespan.Minute
                : MassiveTimespan.Day
        };

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<RequestSnapshot> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RequestSnapshot(
                request.RequestUri,
                request.Headers.Authorization));

            if (_responses.Count == 0)
                throw new InvalidOperationException("No HTTP response was queued for the request.");

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed record RequestSnapshot(Uri? Uri, AuthenticationHeaderValue? Authorization);

    private sealed class StubInstrumentReferential : IInstrumentReferential
    {
        private readonly IReadOnlyList<TickerProviderMapping> _mappings;

        public StubInstrumentReferential(IReadOnlyList<TickerProviderMapping> mappings)
        {
            _mappings = mappings;
        }

        public Task AddMappingsAsync(
            IReadOnlyCollection<TickerProviderMapping> mappings,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TickerProviderMapping>> GetMappingsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_mappings);

        public Task<IReadOnlyList<TickerProviderMapping>> ResolveAsync(
            string ticker,
            string provider,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            var mappings = _mappings
                .Where(item =>
                string.Equals(item.Ticker, ticker, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                item.StartDate <= to.ToDateTime(TimeOnly.MaxValue) &&
                item.EndDate >= from.ToDateTime(TimeOnly.MinValue))
                .OrderBy(item => item.StartDate)
                .ToArray();
            if (mappings.Length == 0)
                throw new TickerReferenceResolutionException(
                    $"No mapping exists for '{ticker}' and provider '{provider}'.");

            var cursor = from;
            foreach (var mapping in mappings)
            {
                var start = DateOnly.FromDateTime(mapping.StartDate);
                var end = DateOnly.FromDateTime(mapping.EndDate);
                if (start > cursor)
                    throw new TickerReferenceResolutionException(
                        $"No mapping covers '{ticker}' for provider '{provider}'.");

                if (end >= to)
                    return Task.FromResult<IReadOnlyList<TickerProviderMapping>>(mappings);

                cursor = end.AddDays(1);
            }

            throw new TickerReferenceResolutionException(
                $"No mapping covers '{ticker}' for provider '{provider}'.");
        }
    }

    #endregion
}
