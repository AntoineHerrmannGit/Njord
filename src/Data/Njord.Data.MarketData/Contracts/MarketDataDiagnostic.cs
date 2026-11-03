using System.Net;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Describes unavailable or unusable market data for a ticker.</summary>
public sealed record MarketDataDiagnostic
{
    #region Diagnostic details

    private string _code = string.Empty;
    private string _message = string.Empty;

    /// <summary>Gets the stable category identifying the diagnostic.</summary>
    public required string Code
    {
        get => _code;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _code = value;
        }
    }

    /// <summary>Gets a human-readable explanation.</summary>
    public required string Message
    {
        get => _message;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _message = value;
        }
    }

    /// <summary>Gets the HTTP status when the provider exposes one.</summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>Gets the number of affected observations, when known.</summary>
    public int? AffectedCount { get; init; }

    /// <summary>Gets the timestamp of the first affected observation, when known.</summary>
    public DateTimeOffset? FirstAffectedTimestamp { get; init; }

    #endregion
}
