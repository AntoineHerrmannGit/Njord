using Njord.Core.Retry;
using Microsoft.Extensions.Options;

namespace Njord.Core.Retry.Tests;

public sealed class RetryPolicyTests
{
    #region Retry execution

    [Fact]
    public async Task ExecuteAsync_RetriesSelectedFailuresUntilSuccess()
    {
        var policy = CreatePolicy();
        var attemptCount = 0;

        var result = await policy.ExecuteAsync(
            _ =>
            {
                attemptCount++;
                return attemptCount < 3
                    ? Task.FromException<int>(new HttpRequestException())
                    : Task.FromResult(42);
            },
            exception => exception is HttpRequestException,
            _ => null);

        Assert.Equal(42, result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryUnselectedFailures()
    {
        var policy = CreatePolicy();
        var attemptCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync(
                _ =>
                {
                    attemptCount++;
                    return Task.FromException<int>(new InvalidOperationException());
                },
                exception => exception is HttpRequestException,
                _ => null));

        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsServerRetryAfterDelay()
    {
        var policy = CreatePolicy();
        var attemptCount = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await policy.ExecuteAsync(
            _ =>
            {
                attemptCount++;
                return attemptCount == 1
                    ? Task.FromException<int>(new HttpRequestException())
                    : Task.FromResult(1);
            },
            exception => exception is HttpRequestException,
            _ => TimeSpan.FromMilliseconds(40));

        Assert.Equal(2, attemptCount);
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(30));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryWhenCancellationIsRequested()
    {
        var policy = CreatePolicy();
        using var cancellationTokenSource = new CancellationTokenSource();
        var attemptCount = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            policy.ExecuteAsync<int>(
                _ =>
                {
                    attemptCount++;
                    cancellationTokenSource.Cancel();
                    return Task.FromCanceled<int>(cancellationTokenSource.Token);
                },
                _ => true,
                _ => null,
                cancellationTokenSource.Token));

        Assert.Equal(1, attemptCount);
    }

    [Theory]
    [InlineData(RetryDelayPattern.Exponential, 700)]
    [InlineData(RetryDelayPattern.Linear, 600)]
    public async Task ExecuteAsync_AppliesConfiguredDelayPattern(
        RetryDelayPattern pattern,
        int expectedMinimumElapsedMilliseconds)
    {
        var policy = CreatePolicy(
            pattern,
            baseDelay: TimeSpan.FromMilliseconds(100),
            maximumDelay: TimeSpan.FromSeconds(1),
            maxRetryAttempts: 3);
        var attemptCount = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await policy.ExecuteAsync(
            _ =>
            {
                attemptCount++;
                return attemptCount < 4
                    ? Task.FromException<int>(new HttpRequestException())
                    : Task.FromResult(1);
            },
            exception => exception is HttpRequestException,
            _ => null);

        Assert.Equal(4, attemptCount);
        Assert.True(stopwatch.ElapsedMilliseconds >= expectedMinimumElapsedMilliseconds);
    }

    [Fact]
    public void RetryPolicyOptionsValidator_RejectsUndefinedPattern()
    {
        var options = new RetryPolicyOptions
        {
            MaxRetryAttempts = 2,
            Pattern = (RetryDelayPattern)99,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaximumDelay = TimeSpan.FromSeconds(1)
        };

        var result = new RetryPolicyOptionsValidator().Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("Pattern", StringComparison.Ordinal));
    }

    #endregion

    #region Test helpers

    private static RetryPolicy CreatePolicy() =>
        CreatePolicy(
            RetryDelayPattern.Exponential,
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(5),
            maxRetryAttempts: 2);

    private static RetryPolicy CreatePolicy(
        RetryDelayPattern pattern,
        TimeSpan baseDelay,
        TimeSpan maximumDelay,
        int maxRetryAttempts) =>
        new(
            Options.Create(new RetryPolicyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Pattern = pattern,
                BaseDelay = baseDelay,
                MaximumDelay = maximumDelay
            }),
            TimeProvider.System);

    #endregion
}
