using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.Core.Retry;
using Njord.DataProviders.YahooFinance.Configuration;
using Njord.DataProviders.YahooFinance.Errors;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Mapping;
using Njord.DataProviders.YahooFinance.Provider;
using Njord.DataProviders.YahooFinance.Transport;
using Njord.Data.Tests.Shared;

namespace Njord.DataProviders.YahooFinance.Tests;

public sealed class YahooFinanceHistoricalMarketDataProviderTests
{
    #region Supported asset classes

    [Theory]
    [InlineData("Stock", "AAPL", "AAPL")]
    [InlineData("Index", "SPX", "^GSPC")]
    [InlineData("Future", "ES=2025-01-17", "ES=F")]
    [InlineData("Crypto", "BTC/USD", "BTC-USD")]
    [InlineData("Forex", "EUR/USD", "EURUSD=X")]
    public async Task GetHistoricalOhlcAsync_QueriesYahooChartAndMapsMarketData(
        string assetClassName,
        string njordTicker,
        string yahooTicker)
    {
        var handler = new QueueHttpMessageHandler(JsonResponse(ChartResponse(yahooTicker)));
        var mapping = CreateMapping(
            njordTicker,
            yahooTicker,
            Enum.Parse<NjordAssetClass>(assetClassName));
        var provider = CreateProvider(handler, [mapping]);

        var results = await provider.GetHistoricalOhlcAsync(
            CreateRequest(njordTicker));

        var sentRequest = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/v8/finance/chart/{yahooTicker}",
            Uri.UnescapeDataString(sentRequest.Uri!.AbsolutePath));
        Assert.Contains("period1=", sentRequest.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("period2=", sentRequest.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("interval=1d", sentRequest.Uri.Query, StringComparison.Ordinal);
        Assert.Equal("Njord/1.0", sentRequest.UserAgent);

        var result = Assert.Single(results);
        Assert.Empty(result.Diagnostics);
        var bar = Assert.Single(result.Bars);
        Assert.Equal(njordTicker, bar.Ticker);
        Assert.Equal(10.25m, bar.Open);
        Assert.Equal(12m, bar.High);
        Assert.Equal(9.5m, bar.Low);
        Assert.Equal(11.75m, bar.Close);
        Assert.Equal(1500m, bar.Volume);
        Assert.Equal("USD", bar.Currency);
        Assert.Null(bar.VolumeWeightedAveragePrice);
        Assert.Null(bar.TransactionCount);
        Assert.Null(bar.IsAdjusted);
        Assert.Equal(TimeSpan.FromDays(1), bar.Interval);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(GetTimestamp()), bar.Timestamp);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_OrdersResultsByRequestTickerAndChronologicalDate()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(ChartResponse("AAPL", GetTimestamp(3))),
            JsonResponse(ChartResponse("MSFT", GetTimestamp(2))));
        var provider = CreateProvider(handler);
        var request = new YahooFinanceHistoricalOhlcRequest
        {
            Tickers = ["AAPL", "MSFT"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = YahooFinanceTimespan.Day
        };

        var results = await provider.GetHistoricalOhlcAsync(request);

        Assert.Equal(["AAPL", "MSFT"], results.Select(result => result.Ticker));
        Assert.Equal(GetTimestamp(3), Assert.Single(results[0].Bars).Timestamp.ToUnixTimeSeconds());
        Assert.Equal(GetTimestamp(2), Assert.Single(results[1].Bars).Timestamp.ToUnixTimeSeconds());
    }

    #endregion

    #region Validation and provider failures

    [Theory]
    [InlineData(YahooFinanceTimespan.Minute, 5, "5m")]
    [InlineData(YahooFinanceTimespan.Hour, 1, "60m")]
    [InlineData(YahooFinanceTimespan.Day, 5, "5d")]
    [InlineData(YahooFinanceTimespan.Week, 1, "1wk")]
    [InlineData(YahooFinanceTimespan.Month, 3, "3mo")]
    [InlineData(YahooFinanceTimespan.Quarter, 1, "3mo")]
    public void YahooFinanceIntervalMapper_MapsSupportedIntervals(
        YahooFinanceTimespan timespan,
        int multiplier,
        string expectedInterval)
    {
        var request = new YahooFinanceHistoricalOhlcRequest
        {
            Tickers = ["AAPL"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = multiplier,
            Timespan = timespan
        };

        var mappedInterval = new YahooFinanceIntervalMapper().Map(request);

        Assert.Equal(expectedInterval, mappedInterval.YahooInterval);
    }

    [Fact]
    public async Task AddYahooFinanceDataProvider_RegistersProviderWithBoundSettings()
    {
        var handler = new QueueHttpMessageHandler(JsonResponse(ChartResponse("AAPL")));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProviders:YahooFinance:BaseAddress"] = "https://query1.finance.yahoo.com/",
                ["DataProviders:YahooFinance:RequestTimeout"] = "00:00:30",
                ["DataProviders:YahooFinance:MaximumBarsPerQuery"] = "100000",
                ["DataProviders:YahooFinance:Retry:Pattern"] = "Exponential",
                ["DataProviders:YahooFinance:Retry:MaxRetryAttempts"] = "0",
                ["DataProviders:YahooFinance:Retry:BaseDelay"] = "00:00:00.001",
                ["DataProviders:YahooFinance:Retry:MaximumDelay"] = "00:00:00.010"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IInstrumentReferential>(
            new StubInstrumentReferential(CreateDefaultMappings()));
        services.AddYahooFinanceDataProvider(configuration);
        services.AddSingleton<IHttpClientFactory>(
            new StubHttpClientFactory(handler, ConfigureYahooHttpClient));
        using var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IYahooFinanceHistoricalMarketDataProvider>();
        var results = await provider.GetHistoricalOhlcAsync(CreateRequest("AAPL"));

        Assert.Single(Assert.Single(results).Bars);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_ReportsUnsupportedNjordAssetClassPerTicker()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(
            handler,
            [CreateMapping("US10Y", "US10Y", NjordAssetClass.Rate)]);

        var results = await provider.GetHistoricalOhlcAsync(CreateRequest("US10Y"));

        var result = Assert.Single(results);
        Assert.Empty(result.Bars);
        Assert.Contains("Rate", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RejectsUnsupportedIntervalBeforeCallingYahoo()
    {
        var handler = new QueueHttpMessageHandler();
        var provider = CreateProvider(handler);
        var request = new YahooFinanceHistoricalOhlcRequest
        {
            Tickers = ["AAPL"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 10,
            Timespan = YahooFinanceTimespan.Minute
        };

        var exception = await Assert.ThrowsAsync<YahooFinanceDataProviderException>(
            () => provider.GetHistoricalOhlcAsync(request));

        Assert.Contains("10-minute", exception.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_KeepsValidBarsAndDiagnosesInvalidBars()
    {
        const string invalidResponse = """
            {
              "chart": {
                "result": [{
                  "meta": { "symbol": "AAPL", "currency": "USD" },
                  "timestamp": [1735828200, 1735914600],
                  "indicators": {
                    "quote": [{
                      "open": [10, 12],
                      "high": [12, 10],
                      "low": [9, 11],
                      "close": [11, 11],
                      "volume": [1500, 1600]
                    }]
                  }
                }],
                "error": null
              }
            }
            """;
        var provider = CreateProvider(new QueueHttpMessageHandler(JsonResponse(invalidResponse)));

        var result = Assert.Single(await provider.GetHistoricalOhlcAsync(CreateRequest("AAPL")));

        Assert.Single(result.Bars);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("InvalidOhlc", diagnostic.Code);
        Assert.Equal(1, diagnostic.AffectedBarCount);
        Assert.Equal(GetTimestamp(3), diagnostic.FirstAffectedTimestamp!.Value.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_ReportsYahooChartErrorsPerTicker()
    {
        const string errorResponse = """
            {
              "chart": {
                "result": null,
                "error": { "code": "Not Found", "description": "No data found" }
              }
            }
            """;
        var provider = CreateProvider(new QueueHttpMessageHandler(JsonResponse(errorResponse)));

        var result = Assert.Single(await provider.GetHistoricalOhlcAsync(CreateRequest("AAPL")));

        Assert.Empty(result.Bars);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("Not Found", diagnostic.Code);
        Assert.Contains("No data found", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_ReportsEmptyChartWithoutCreatingBars()
    {
        const string noDataResponse = """
            {
              "chart": {
                "result": [{
                  "meta": { "symbol": "AAPL", "currency": "USD" },
                  "timestamp": [],
                  "indicators": {
                    "quote": [{
                      "open": [],
                      "high": [],
                      "low": [],
                      "close": [],
                      "volume": []
                    }]
                  }
                }],
                "error": null
              }
            }
            """;
        var provider = CreateProvider(new QueueHttpMessageHandler(JsonResponse(noDataResponse)));

        var result = Assert.Single(await provider.GetHistoricalOhlcAsync(CreateRequest("AAPL")));

        Assert.Empty(result.Bars);
        Assert.Equal("NoData", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_RetriesTransientHttpResponses()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            JsonResponse(ChartResponse("AAPL")));
        var provider = CreateProvider(handler, retryAttempts: 1);

        var results = await provider.GetHistoricalOhlcAsync(CreateRequest("AAPL"));

        Assert.Single(Assert.Single(results).Bars);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetHistoricalOhlcAsync_KeepsAvailableTickersWhenOptionHistoryIsUnavailable()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound),
            JsonResponse(ChartResponse("AAPL")));
        var provider = CreateProvider(handler);
        var request = new YahooFinanceHistoricalOhlcRequest
        {
            Tickers = ["AAPL-2025-01-17-C-100", "AAPL"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = YahooFinanceTimespan.Day
        };

        var results = await provider.GetHistoricalOhlcAsync(request);

        Assert.Equal(["AAPL-2025-01-17-C-100", "AAPL"], results.Select(result => result.Ticker));
        Assert.Empty(results[0].Bars);
        Assert.Equal(HttpStatusCode.NotFound, Assert.Single(results[0].Diagnostics).StatusCode);
        Assert.Single(results[1].Bars);
        Assert.Empty(results[1].Diagnostics);
        Assert.Equal(2, handler.Requests.Count);
    }

    #endregion

    #region Test fixtures

    private static YahooFinanceHistoricalOhlcRequest CreateRequest(string ticker) =>
        new()
        {
            Tickers = [ticker],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Timespan = YahooFinanceTimespan.Day
        };

    private static YahooFinanceHistoricalMarketDataProvider CreateProvider(
        QueueHttpMessageHandler handler,
        IReadOnlyList<TickerProviderMapping>? mappings = null,
        int retryAttempts = 0)
    {
        var options = new YahooFinanceDataProviderOptions
        {
            BaseAddress = new Uri("https://query1.finance.yahoo.com/"),
            MaximumBarsPerQuery = 100_000,
            Retry = new RetryPolicyOptions
            {
                MaxRetryAttempts = retryAttempts,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                MaximumDelay = TimeSpan.FromMilliseconds(10)
            }
        };
        var apiClient = new YahooFinanceChartApiClient(
            new StubHttpClientFactory(handler, ConfigureYahooHttpClient),
            Options.Create(options),
            TimeProvider.System);

        return new YahooFinanceHistoricalMarketDataProvider(
            apiClient,
            new YahooFinanceOhlcMapper(),
            new YahooFinanceIntervalMapper(),
            new StubInstrumentReferential(mappings ?? CreateDefaultMappings()),
            Options.Create(options),
            NullLogger<YahooFinanceHistoricalMarketDataProvider>.Instance);
    }

    private static void ConfigureYahooHttpClient(HttpClient client)
    {
        client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Njord/1.0");
    }

    private static IReadOnlyList<TickerProviderMapping> CreateDefaultMappings() =>
    [
        CreateMapping("AAPL", "AAPL", NjordAssetClass.Stock),
        CreateMapping("MSFT", "MSFT", NjordAssetClass.Stock),
        CreateMapping("SPX", "^GSPC", NjordAssetClass.Index),
        CreateMapping("AAPL-2025-01-17-C-100", "AAPL250117C00100000", NjordAssetClass.Option),
        CreateMapping("ES=2025-01-17", "ES=F", NjordAssetClass.Future),
        CreateMapping("BTC/USD", "BTC-USD", NjordAssetClass.Crypto),
        CreateMapping("EUR/USD", "EURUSD=X", NjordAssetClass.Forex)
    ];

    private static TickerProviderMapping CreateMapping(
        string ticker,
        string providerTicker,
        NjordAssetClass assetClass) =>
        new()
        {
            Ticker = ticker,
            Provider = "YahooFinance",
            ProviderTicker = providerTicker,
            AssetClass = assetClass,
            StartDate = DateTime.MinValue,
            EndDate = DateTime.MaxValue
        };

    private static string ChartResponse(string symbol, long timestamp = 1735828200) =>
        $$"""
          {
            "chart": {
              "result": [{
                "meta": { "symbol": "{{symbol}}", "currency": "USD" },
                "timestamp": [{{timestamp}}],
                "indicators": {
                  "quote": [{
                    "open": [10.25],
                    "high": [12],
                    "low": [9.5],
                    "close": [11.75],
                    "volume": [1500]
                  }]
                }
              }],
              "error": null
            }
          }
          """;

    private static long GetTimestamp(int day = 2) =>
        new DateTimeOffset(2025, 1, day, 14, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();

    private static HttpResponseMessage JsonResponse(string json) =>
        HttpTestResponses.JsonResponse(json);

    private sealed class QueueHttpMessageHandler : QueuedHttpMessageHandler<RequestSnapshot>
    {
        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
            : base(
                responses,
                request => new RequestSnapshot(
                    request.RequestUri,
                    request.Headers.UserAgent.ToString()))
        {
        }

    }

    private sealed record RequestSnapshot(Uri? Uri, string UserAgent);

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
