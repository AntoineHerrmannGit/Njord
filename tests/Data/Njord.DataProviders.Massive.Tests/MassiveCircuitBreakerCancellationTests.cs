using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Njord.Core.Retry;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;
using Njord.DataProviders.Massive.Transport;
using Njord.Data.Tests.Shared;

namespace Njord.DataProviders.Massive.Tests;

public sealed class MassiveCircuitBreakerCancellationTests
{
    #region Half-open probe cancellation

    [Fact]
    public async Task CancelledProbe_ReleasesProbeAndKeepsCircuitOpenUntilCooldownAndSuccess()
    {
        var openedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeProvider = new ControllableTimeProvider(openedAt);
        var options = Options.Create(new MassiveDataProviderOptions
        {
            BaseAddress = new Uri("https://api.massive.test/"),
            ApiKey = "test-api-key",
            RequestTimeout = TimeSpan.FromSeconds(5),
            CircuitBreakerFailureThreshold = 1,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10),
            MaximumPagesPerQuery = 10
        });
        var circuitBreaker = new MassiveCircuitBreaker(options, timeProvider);
        circuitBreaker.RecordTransientFailure();
        timeProvider.SetUtcNow(openedAt.AddSeconds(10));

        var handler = new BlockingFirstRequestHandler();
        var apiClient = new MassiveAggregatesApiClient(
            new StubHttpClientFactory(handler),
            options,
            new RetryPolicy(
                Options.Create(new RetryPolicyOptions
                {
                    MaxRetryAttempts = 0,
                    BaseDelay = TimeSpan.FromMilliseconds(1),
                    MaximumDelay = TimeSpan.FromSeconds(1)
                }),
                timeProvider),
            circuitBreaker,
            timeProvider);
        var request = new MassiveResolvedOhlcRequest
        {
            NjordTicker = "AAPL",
            Ticker = "AAPL",
            AssetClass = MassiveAssetClass.Stock,
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 2),
            Multiplier = 1,
            Timespan = MassiveTimespan.Day
        };

        using var cancellationTokenSource = new CancellationTokenSource();
        var probeTask = apiClient.GetHistoricalAggregatesAsync(request, cancellationTokenSource.Token);
        await handler.FirstRequestStarted.Task;

        await Assert.ThrowsAsync<MassiveCircuitOpenException>(
            () => apiClient.GetHistoricalAggregatesAsync(request, CancellationToken.None));

        cancellationTokenSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => probeTask);

        timeProvider.SetUtcNow(openedAt.AddSeconds(9));
        await Assert.ThrowsAsync<MassiveCircuitOpenException>(
            () => apiClient.GetHistoricalAggregatesAsync(request, CancellationToken.None));
        Assert.Equal(1, handler.RequestCount);

        timeProvider.SetUtcNow(openedAt.AddSeconds(10));
        var pages = await apiClient.GetHistoricalAggregatesAsync(request, CancellationToken.None);

        Assert.Single(pages);
        Assert.Empty(Assert.Single(pages).Results!);
        Assert.Equal(2, handler.RequestCount);
    }

    #endregion

    #region Test helpers

    private sealed class ControllableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ControllableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    }

    private sealed class BlockingFirstRequestHandler : HttpMessageHandler
    {
        private int _requestCount;

        public TaskCompletionSource FirstRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RequestCount => _requestCount;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _requestCount) == 1)
            {
                FirstRequestStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"status":"OK","results":[]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    #endregion
}
