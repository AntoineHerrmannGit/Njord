using System.Net;

namespace Njord.DataProviders.Massive.Errors;

internal sealed class MassiveTransientHttpException : Exception
{
    #region Transient response details

    public MassiveTransientHttpException(
        HttpStatusCode statusCode,
        string? requestId,
        TimeSpan? retryAfter)
        : base($"Massive returned transient HTTP status {(int)statusCode}.")
    {
        StatusCode = statusCode;
        RequestId = requestId;
        RetryAfter = retryAfter;
    }

    public HttpStatusCode StatusCode { get; }

    public string? RequestId { get; }

    public TimeSpan? RetryAfter { get; }

    #endregion
}
