namespace Njord.Core.Retry;

/// <summary>Executes asynchronous operations with bounded retries and exponential delays.</summary>
public sealed class RetryPolicy
{
    #region Policy configuration

    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maximumDelay;
    private readonly TimeProvider _timeProvider;

    public RetryPolicy(RetryPolicyOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (options.MaxRetryAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetryAttempts cannot be negative.");

        if (options.BaseDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "BaseDelay must be positive.");

        if (options.MaximumDelay < options.BaseDelay)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaximumDelay must be greater than or equal to BaseDelay.");

        _maxRetryAttempts = options.MaxRetryAttempts;
        _baseDelay = options.BaseDelay;
        _maximumDelay = options.MaximumDelay;
        _timeProvider = timeProvider;
    }

    #endregion

    #region Retry execution

    /// <summary>Executes an asynchronous operation and retries exceptions selected by the caller.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="shouldRetry">Selects exceptions that may be retried.</param>
    /// <param name="getRetryAfter">Returns an optional server-provided delay for an exception.</param>
    /// <param name="cancellationToken">Cancels the operation or a retry delay.</param>
    /// <returns>The result of the first successful attempt.</returns>
    /// <exception cref="ArgumentNullException">A delegate is null.</exception>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Exception, bool> shouldRetry,
        Func<Exception, TimeSpan?> getRetryAfter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(shouldRetry);
        ArgumentNullException.ThrowIfNull(getRetryAfter);

        var retryCount = 0;
        while (true)
        {
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (
                retryCount < _maxRetryAttempts &&
                shouldRetry(exception))
            {
                var delay = GetDelay(retryCount, getRetryAfter(exception));
                retryCount++;
                await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private TimeSpan GetDelay(int retryCount, TimeSpan? retryAfter)
    {
        var delay = _baseDelay;
        for (var retry = 0; retry < retryCount && delay < _maximumDelay; retry++)
        {
            delay = delay.Ticks > _maximumDelay.Ticks / 2
                ? _maximumDelay
                : TimeSpan.FromTicks(delay.Ticks * 2);
        }

        if (delay > _maximumDelay)
            delay = _maximumDelay;

        if (retryAfter > delay)
            delay = retryAfter.Value;

        return delay;
    }

    #endregion
}
