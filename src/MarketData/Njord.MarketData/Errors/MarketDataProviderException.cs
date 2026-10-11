using System.Net;

namespace Njord.MarketData;

/// <summary>Represents a failed or invalid response from a market-data provider.</summary>
public sealed class MarketDataProviderException : Exception
{
    /// <summary>Creates a provider exception.</summary>
    public MarketDataProviderException(
        string providerId,
        string message,
        HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ProviderId = providerId;
        StatusCode = statusCode;
    }

    /// <summary>Gets the identifier of the provider that failed.</summary>
    public string ProviderId { get; }

    /// <summary>Gets the HTTP status code when the provider returned an unsuccessful response.</summary>
    public HttpStatusCode? StatusCode { get; }
}
