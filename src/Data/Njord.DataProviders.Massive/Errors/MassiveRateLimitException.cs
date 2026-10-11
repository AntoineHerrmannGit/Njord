using System.Net;

namespace Njord.DataProviders.Massive.Errors;

/// <summary>Indicates that Massive rejected a request because the account rate limit was exceeded.</summary>
public sealed class MassiveRateLimitException : MassiveDataProviderException
{
    #region Rate limit details

    public MassiveRateLimitException(TimeSpan? retryAfter, string? requestId)
        : base("Massive rejected the request due to its account rate limit.", HttpStatusCode.TooManyRequests, requestId)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>Gets the server-provided retry delay, when available.</summary>
    public TimeSpan? RetryAfter { get; }

    #endregion
}
