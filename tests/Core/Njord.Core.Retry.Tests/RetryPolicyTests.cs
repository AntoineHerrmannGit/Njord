using Njord.Core.Retry;

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

    #endregion

    #region Test helpers

    private static RetryPolicy CreatePolicy() =>
        new(
            new RetryPolicyOptions
            {
                MaxRetryAttempts = 2,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                MaximumDelay = TimeSpan.FromMilliseconds(5)
            },
            TimeProvider.System);

    #endregion
}
