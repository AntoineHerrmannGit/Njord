using System.Net;

namespace Njord.DataProviders.Massive.Errors;

/// <summary>Describes a failed request or invalid response from Massive.</summary>
public class MassiveDataProviderException : Exception
{
    #region Error details

    public MassiveDataProviderException(
        string message,
        HttpStatusCode? statusCode = null,
        string? requestId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RequestId = requestId;
    }

    /// <summary>Gets the HTTP status code when a response was received.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets the Massive request identifier when supplied by the API.</summary>
    public string? RequestId { get; }

    #endregion
}
