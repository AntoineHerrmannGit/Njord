using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Contracts;

/// <summary>Describes a vector request for corporate actions associated with canonical tickers.</summary>
public sealed record CorporateActionHistoricalRequest : IMarketDataRequest<MarketCorporateAction>
{
    #region Request identity

    /// <summary>Gets the canonical Njord tickers whose corporate actions are requested.</summary>
    public required IReadOnlyList<string> Tickers { get; init; }

    #endregion

    #region Historical range

    /// <summary>Gets the first requested date, inclusive.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last requested date, inclusive.</summary>
    public required DateOnly To { get; init; }

    #endregion

    #region Validation

    /// <summary>Validates the common vector request values.</summary>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Tickers);
        if (Tickers.Count == 0)
            throw new ArgumentException("At least one Njord ticker is required.", nameof(Tickers));

        var uniqueTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ticker in Tickers)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
            if (!uniqueTickers.Add(ticker.Trim()))
                throw new ArgumentException($"Ticker '{ticker}' occurs more than once in the request.", nameof(Tickers));
        }

        if (From > To)
            throw new ArgumentException("The start date must not be after the end date.");

        if (To == DateOnly.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(To), To, "The end date must allow an exclusive next-day boundary.");
    }

    #endregion
}
