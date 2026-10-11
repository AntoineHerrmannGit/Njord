using Microsoft.Extensions.Options;
using Njord.DataProviders.Massive.Configuration;

namespace Njord.DataProviders.Massive.Transport;

internal sealed class MassiveRequestRateLimiter
{
    #region Rate limiting

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _minimumRequestSpacing;
    private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

    public MassiveRequestRateLimiter(
        IOptions<MassiveDataProviderOptions> options,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var ticksPerRequest = TimeSpan.TicksPerMinute / options.Value.RequestsPerMinute;
        _minimumRequestSpacing = TimeSpan.FromTicks(Math.Max(1, ticksPerRequest));
    }

    public async ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var delay = _nextRequestAt - _timeProvider.GetUtcNow();
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);

            _nextRequestAt = _timeProvider.GetUtcNow() + _minimumRequestSpacing;
        }
        finally
        {
            _gate.Release();
        }
    }

    #endregion
}
