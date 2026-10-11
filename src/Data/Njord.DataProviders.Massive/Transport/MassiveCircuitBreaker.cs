using Microsoft.Extensions.Options;
using Njord.DataProviders.Massive.Configuration;
using Njord.DataProviders.Massive.Errors;

namespace Njord.DataProviders.Massive.Transport;

internal sealed class MassiveCircuitBreaker
{
    #region Circuit state

    private readonly object _gate = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;
    private readonly TimeProvider _timeProvider;
    private int _consecutiveFailures;
    private DateTimeOffset? _openedAt;
    private bool _probeInProgress;

    public MassiveCircuitBreaker(
        IOptions<MassiveDataProviderOptions> options,
        TimeProvider timeProvider)
    {
        _failureThreshold = options.Value.CircuitBreakerFailureThreshold;
        _breakDuration = options.Value.CircuitBreakerBreakDuration;
        _timeProvider = timeProvider;
    }

    public void ThrowIfOpen()
    {
        lock (_gate)
        {
            if (!_openedAt.HasValue)
                return;

            var retryAfter = _openedAt.Value + _breakDuration - _timeProvider.GetUtcNow();
            if (retryAfter > TimeSpan.Zero)
                throw new MassiveCircuitOpenException(retryAfter);

            if (_probeInProgress)
                throw new MassiveCircuitOpenException(TimeSpan.Zero);

            _probeInProgress = true;
        }
    }

    public void RecordSuccess()
    {
        lock (_gate)
        {
            _consecutiveFailures = 0;
            _openedAt = null;
            _probeInProgress = false;
        }
    }

    public void RecordTransientFailure()
    {
        lock (_gate)
        {
            _consecutiveFailures++;
            if (_probeInProgress || _consecutiveFailures >= _failureThreshold)
            {
                _openedAt = _timeProvider.GetUtcNow();
                _consecutiveFailures = 0;
                _probeInProgress = false;
            }
        }
    }

    #endregion
}
