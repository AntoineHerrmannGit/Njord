using System.Net;

namespace Njord.DataProviders.YahooFinance.Contracts;

/// <summary>Describes market data that Yahoo Finance did not provide or that the adapter could not use.</summary>
public sealed record YahooFinanceDataDiagnostic
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

    /// <summary>Gets the HTTP status code when Yahoo Finance returned an HTTP error.</summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>Gets the number of bars omitted for this data-quality diagnostic.</summary>
    public int? AffectedBarCount { get; init; }

    /// <summary>Gets the first affected bar timestamp, when available.</summary>
    public DateTimeOffset? FirstAffectedTimestamp { get; init; }

    #endregion
}
