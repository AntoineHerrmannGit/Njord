using Microsoft.Extensions.Options;

namespace Njord.Core.Retry;

/// <summary>Executes asynchronous operations with bounded retries and configurable delay patterns.</summary>
public sealed class RetryPolicy
{
    #region Policy configuration

    private readonly int _maxRetryAttempts;
    private readonly RetryDelayPattern _pattern;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maximumDelay;
    private readonly TimeProvider _timeProvider;

    public RetryPolicy(IOptions<RetryPolicyOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (!Enum.IsDefined(options.Value.Pattern))
            throw new ArgumentOutOfRangeException(nameof(options), "Pattern must be a defined retry delay pattern.");

        _maxRetryAttempts = options.Value.MaxRetryAttempts;
        _pattern = options.Value.Pattern;
        _baseDelay = options.Value.BaseDelay;
        _maximumDelay = options.Value.MaximumDelay;
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
        var delay = _pattern switch
        {
            RetryDelayPattern.Exponential => GetExponentialDelay(retryCount),
            RetryDelayPattern.Linear => GetLinearDelay(retryCount),
            _ => throw new InvalidOperationException($"Unsupported retry delay pattern: {_pattern}.")
        };

        if (retryAfter > delay)
            delay = retryAfter.Value;

        return delay;
    }

    private TimeSpan GetExponentialDelay(int retryCount)
    {
        var delay = _baseDelay;
        for (var retry = 0; retry < retryCount && delay < _maximumDelay; retry++)
        {
            delay = delay.Ticks > _maximumDelay.Ticks / 2
                ? _maximumDelay
                : TimeSpan.FromTicks(delay.Ticks * 2);
        }

        return delay > _maximumDelay ? _maximumDelay : delay;
    }

    private TimeSpan GetLinearDelay(int retryCount)
    {
        var multiplier = (long)retryCount + 1;
        if (_baseDelay.Ticks > _maximumDelay.Ticks / multiplier)
            return _maximumDelay;

        var delay = TimeSpan.FromTicks(_baseDelay.Ticks * multiplier);
        return delay > _maximumDelay ? _maximumDelay : delay;
    }

    #endregion
}
