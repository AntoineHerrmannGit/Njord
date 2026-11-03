using System.Net;

namespace Njord.DataProviders.YahooFinance.Errors;

/// <summary>Describes an unsupported Yahoo Finance request or failed request/response.</summary>
public class YahooFinanceDataProviderException : Exception
{
    #region Error details

    /// <summary>Initializes an exception describing a Yahoo Finance provider failure.</summary>
    public YahooFinanceDataProviderException(
        string message,
        HttpStatusCode? statusCode = null,
        string? errorCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the HTTP status code when a response was received.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets the Yahoo Finance error code when supplied by the API.</summary>
    public string? ErrorCode { get; }

    #endregion
}
