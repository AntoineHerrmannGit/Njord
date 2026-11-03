namespace Njord.Data.MarketData.Contracts;

/// <summary>Describes why an OHLC comparison could not be fully validated.</summary>
public sealed record MarketDataComparisonDiagnostic
{
    #region Diagnostic details

    private string _code = string.Empty;
    private string _message = string.Empty;

    /// <summary>Gets the stable category identifying the comparison diagnostic.</summary>
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

    #endregion
}
