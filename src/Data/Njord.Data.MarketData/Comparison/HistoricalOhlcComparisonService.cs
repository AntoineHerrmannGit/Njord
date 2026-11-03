using Njord.Data.MarketData.Contracts;
using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Comparison;

/// <summary>Compares provider OHLC results without altering or adjusting market data.</summary>
public static class HistoricalOhlcComparisonService
{
    #region Comparison policy

    /// <summary>Gets the fixed maximum passing percentage difference.</summary>
    public const decimal TolerancePercentage = 1m;

    /// <summary>
    /// Compares per-ticker OHLC results using the shared request's interval and ticker order.
    /// A bar's start instant is <see cref="Ohlc.PeriodStart"/> when supplied, otherwise
    /// <see cref="Ohlc.Timestamp"/>.
    /// </summary>
    /// <param name="request">The common request used to retrieve both result vectors.</param>
    /// <param name="leftResults">The first provider's per-ticker results.</param>
    /// <param name="rightResults">The second provider's per-ticker results.</param>
    /// <returns>One comparison result for each requested ticker, in request order.</returns>
    public static IReadOnlyList<MarketDataTickerOhlcComparisonResult> Compare(
        MarketDataHistoricalOhlcRequest request,
        IReadOnlyList<MarketDataTickerOhlcResult> leftResults,
        IReadOnlyList<MarketDataTickerOhlcResult> rightResults)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(leftResults);
        ArgumentNullException.ThrowIfNull(rightResults);
        request.Validate();

        var output = new MarketDataTickerOhlcComparisonResult[request.Tickers.Count];
        for (var index = 0; index < request.Tickers.Count; index++)
        {
            var ticker = request.Tickers[index];
            output[index] = CompareTicker(
                ticker,
                request.Interval,
                leftResults,
                rightResults);
        }

        return Array.AsReadOnly(output);
    }

    #endregion

    #region Ticker comparison

    private static MarketDataTickerOhlcComparisonResult CompareTicker(
        string ticker,
        MarketDataInterval interval,
        IReadOnlyList<MarketDataTickerOhlcResult> leftResults,
        IReadOnlyList<MarketDataTickerOhlcResult> rightResults)
    {
        var diagnostics = new List<MarketDataComparisonDiagnostic>();
        var leftResult = FindUniqueTickerResult(ticker, leftResults, "first", diagnostics);
        var rightResult = FindUniqueTickerResult(ticker, rightResults, "second", diagnostics);
        var leftBars = leftResult?.Bars ?? Array.Empty<Ohlc>();
        var rightBars = rightResult?.Bars ?? Array.Empty<Ohlc>();

        AddProviderDiagnostics(leftResult, "first", diagnostics);
        AddProviderDiagnostics(rightResult, "second", diagnostics);

        var leftTimeZone = ResolveTimeZone(ticker, leftResult, "first", diagnostics);
        var rightTimeZone = ResolveTimeZone(ticker, rightResult, "second", diagnostics);
        var leftIndex = IndexBars(ticker, leftBars, interval, leftTimeZone, "first", diagnostics);
        var rightIndex = IndexBars(ticker, rightBars, interval, rightTimeZone, "second", diagnostics);
        var matchedBars = MatchBars(
            ticker,
            interval,
            leftIndex,
            rightIndex,
            leftResult,
            rightResult,
            leftTimeZone,
            rightTimeZone,
            diagnostics);

        var matchedBarCount = matchedBars.Count;
        var leftBarCount = leftBars.Count;
        var rightBarCount = rightBars.Count;
        if (matchedBarCount == 0)
            AddDiagnostic(diagnostics, "NoMatchedBars", $"No bars were matched for ticker '{ticker}'.");

        if (leftBarCount > matchedBarCount || rightBarCount > matchedBarCount)
        {
            AddDiagnostic(
                diagnostics,
                "UnmatchedBars",
                $"Ticker '{ticker}' has {leftBarCount - matchedBarCount} unmatched first-provider bars and " +
                $"{rightBarCount - matchedBarCount} unmatched second-provider bars.");
        }

        var comparedBarCount = matchedBars.Count(
            bar => bar.Fields.All(field => field.Status != MarketDataComparisonStatus.Inconclusive));
        var anyFieldFailed = matchedBars.Any(
            bar => bar.Fields.Any(field => field.Status == MarketDataComparisonStatus.Fail));
        var status = anyFieldFailed
            ? MarketDataComparisonStatus.Fail
            : diagnostics.Count > 0 || matchedBarCount == 0 ||
              matchedBars.Any(bar => bar.Status == MarketDataComparisonStatus.Inconclusive)
                ? MarketDataComparisonStatus.Inconclusive
                : MarketDataComparisonStatus.Pass;

        return new MarketDataTickerOhlcComparisonResult
        {
            Ticker = ticker,
            LeftBarCount = leftBarCount,
            RightBarCount = rightBarCount,
            MatchedBarCount = matchedBarCount,
            ComparedBarCount = comparedBarCount,
            MatchedBars = matchedBars,
            Status = status,
            Diagnostics = diagnostics
        };
    }

    private static MarketDataTickerOhlcResult? FindUniqueTickerResult(
        string ticker,
        IReadOnlyList<MarketDataTickerOhlcResult> results,
        string side,
        ICollection<MarketDataComparisonDiagnostic> diagnostics)
    {
        var matches = results
            .Where(result => string.Equals(result.Ticker, ticker, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (matches.Length == 1)
            return matches[0];

        AddDiagnostic(
            diagnostics,
            matches.Length == 0 ? "MissingTickerResult" : "DuplicateTickerResult",
            matches.Length == 0
                ? $"The {side} provider returned no result for ticker '{ticker}'."
                : $"The {side} provider returned multiple results for ticker '{ticker}'.");
        return null;
    }

    private static void AddProviderDiagnostics(
        MarketDataTickerOhlcResult? result,
        string side,
        ICollection<MarketDataComparisonDiagnostic> diagnostics)
    {
        if (result is null)
            return;

        foreach (var diagnostic in result.Diagnostics)
            AddDiagnostic(
                diagnostics,
                "ProviderDiagnostic",
                $"The {side} provider reported '{diagnostic.Code}': {diagnostic.Message}");
    }

    private static TimeZoneInfo? ResolveTimeZone(
        string ticker,
        MarketDataTickerOhlcResult? result,
        string side,
        ICollection<MarketDataComparisonDiagnostic> diagnostics)
    {
        if (result is null || string.IsNullOrWhiteSpace(result.ExchangeTimeZoneId))
        {
            AddDiagnostic(
                diagnostics,
                "MissingExchangeTimeZone",
                $"The {side} provider supplied no exchange timezone for ticker '{ticker}'.");
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(result.ExchangeTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            AddDiagnostic(
                diagnostics,
                "InvalidExchangeTimeZone",
                $"The {side} provider supplied an unknown exchange timezone for ticker '{ticker}'.");
        }
        catch (InvalidTimeZoneException)
        {
            AddDiagnostic(
                diagnostics,
                "InvalidExchangeTimeZone",
                $"The {side} provider supplied an invalid exchange timezone for ticker '{ticker}'.");
        }

        return null;
    }

    #endregion

    #region Bar matching and field comparison

    private static Dictionary<BarMatchKey, List<IndexedBar>> IndexBars(
        string ticker,
        IReadOnlyList<Ohlc> bars,
        MarketDataInterval interval,
        TimeZoneInfo? timeZone,
        string side,
        ICollection<MarketDataComparisonDiagnostic> diagnostics)
    {
        var index = new Dictionary<BarMatchKey, List<IndexedBar>>();
        foreach (var bar in bars)
        {
            if (!string.Equals(bar.Ticker, ticker, StringComparison.OrdinalIgnoreCase))
            {
                AddDiagnostic(
                    diagnostics,
                    "BarTickerMismatch",
                    $"A {side} provider bar in ticker result '{ticker}' identifies ticker '{bar.Ticker}'.");
                continue;
            }

            var start = bar.PeriodStart ?? bar.Timestamp;
            BarMatchKey key;
            DateOnly? exchangeLocalDate = null;
            if (interval == MarketDataInterval.Day)
            {
                if (timeZone is null)
                    continue;

                exchangeLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, timeZone).DateTime);
                key = BarMatchKey.ForDate(exchangeLocalDate.Value);
            }
            else
                key = BarMatchKey.ForInstant(start.UtcDateTime.Ticks);

            if (!index.TryGetValue(key, out var matchingBars))
            {
                matchingBars = [];
                index.Add(key, matchingBars);
            }

            matchingBars.Add(new IndexedBar(bar, start, exchangeLocalDate));
        }

        foreach (var duplicate in index.Where(pair => pair.Value.Count > 1))
            AddDiagnostic(
                diagnostics,
                "DuplicateMatchKey",
                $"The {side} provider returned {duplicate.Value.Count} bars with the same match key for ticker '{ticker}'.");

        return index;
    }

    private static List<MarketDataOhlcBarComparison> MatchBars(
        string ticker,
        MarketDataInterval interval,
        IReadOnlyDictionary<BarMatchKey, List<IndexedBar>> leftIndex,
        IReadOnlyDictionary<BarMatchKey, List<IndexedBar>> rightIndex,
        MarketDataTickerOhlcResult? leftResult,
        MarketDataTickerOhlcResult? rightResult,
        TimeZoneInfo? leftTimeZone,
        TimeZoneInfo? rightTimeZone,
        ICollection<MarketDataComparisonDiagnostic> diagnostics)
    {
        var output = new List<MarketDataOhlcBarComparison>();
        foreach (var (key, leftBars) in leftIndex)
        {
            if (leftBars.Count != 1 ||
                !rightIndex.TryGetValue(key, out var rightBars) ||
                rightBars.Count != 1)
                continue;

            var leftBar = leftBars[0];
            var rightBar = rightBars[0];
            var comparisonAllowed = CanCompareBar(
                ticker,
                leftBar.Bar,
                rightBar.Bar,
                leftResult,
                rightResult,
                leftTimeZone,
                rightTimeZone,
                diagnostics);
            var fields = CompareFields(leftBar.Bar, rightBar.Bar, comparisonAllowed);
            output.Add(new MarketDataOhlcBarComparison
            {
                LeftBarStart = leftBar.Start,
                RightBarStart = rightBar.Start,
                ExchangeLocalDate = interval == MarketDataInterval.Day
                    ? leftBar.ExchangeLocalDate
                    : null,
                Fields = fields
            });
        }

        return output;
    }

    private static bool CanCompareBar(
        string ticker,
        Ohlc leftBar,
        Ohlc rightBar,
        MarketDataTickerOhlcResult? leftResult,
        MarketDataTickerOhlcResult? rightResult,
        TimeZoneInfo? leftTimeZone,
        TimeZoneInfo? rightTimeZone,
        ICollection<MarketDataComparisonDiagnostic> diagnostics)
    {
        var canCompare = leftResult is not null &&
                         rightResult is not null &&
                         leftTimeZone is not null &&
                         rightTimeZone is not null;
        if (leftBar.IsAdjusted != false || rightBar.IsAdjusted != false)
        {
            AddDiagnostic(
                diagnostics,
                leftBar.IsAdjusted is null || rightBar.IsAdjusted is null
                    ? "UnknownPriceBasis"
                    : "AdjustedPrices",
                $"Ticker '{ticker}' requires both bars to explicitly report unadjusted prices.");
            canCompare = false;
        }

        var leftCurrencyKnown = !string.IsNullOrWhiteSpace(leftBar.Currency);
        var rightCurrencyKnown = !string.IsNullOrWhiteSpace(rightBar.Currency);
        if (!leftCurrencyKnown || !rightCurrencyKnown)
        {
            AddDiagnostic(
                diagnostics,
                "UnknownCurrency",
                $"Ticker '{ticker}' requires a known currency on both matched bars.");
            canCompare = false;
        }
        else if (!string.Equals(leftBar.Currency, rightBar.Currency, StringComparison.OrdinalIgnoreCase))
        {
            AddDiagnostic(
                diagnostics,
                "CurrencyMismatch",
                $"Ticker '{ticker}' has matched bars quoted in different currencies.");
            canCompare = false;
        }

        return canCompare;
    }

    private static IReadOnlyList<MarketDataOhlcFieldComparison> CompareFields(
        Ohlc left,
        Ohlc right,
        bool comparisonAllowed)
    {
        var fields = new[]
        {
            CompareField(MarketDataOhlcField.Open, left.Open, right.Open, comparisonAllowed),
            CompareField(MarketDataOhlcField.High, left.High, right.High, comparisonAllowed),
            CompareField(MarketDataOhlcField.Low, left.Low, right.Low, comparisonAllowed),
            CompareField(MarketDataOhlcField.Close, left.Close, right.Close, comparisonAllowed)
        };
        return Array.AsReadOnly(fields);
    }

    private static MarketDataOhlcFieldComparison CompareField(
        MarketDataOhlcField field,
        decimal left,
        decimal right,
        bool comparisonAllowed)
    {
        if (!comparisonAllowed)
            return new MarketDataOhlcFieldComparison
            {
                Field = field,
                PercentageDifference = null,
                Status = MarketDataComparisonStatus.Inconclusive
            };

        if (left == 0m || right == 0m)
        {
            var bothZero = left == 0m && right == 0m;
            return new MarketDataOhlcFieldComparison
            {
                Field = field,
                PercentageDifference = bothZero ? 0m : null,
                Status = bothZero ? MarketDataComparisonStatus.Pass : MarketDataComparisonStatus.Fail
            };
        }

        var percentage = 100m * Math.Abs(left - right) /
                         Math.Min(Math.Abs(left), Math.Abs(right));
        return new MarketDataOhlcFieldComparison
        {
            Field = field,
            PercentageDifference = percentage,
            Status = percentage > TolerancePercentage
                ? MarketDataComparisonStatus.Fail
                : MarketDataComparisonStatus.Pass
        };
    }

    #endregion

    #region Diagnostics and private types

    private static void AddDiagnostic(
        ICollection<MarketDataComparisonDiagnostic> diagnostics,
        string code,
        string message) =>
        diagnostics.Add(new MarketDataComparisonDiagnostic { Code = code, Message = message });

    private readonly record struct BarMatchKey(DateOnly? ExchangeLocalDate, long? UtcTicks)
    {
        public static BarMatchKey ForDate(DateOnly date) => new(date, null);

        public static BarMatchKey ForInstant(long utcTicks) => new(null, utcTicks);
    }

    private sealed record IndexedBar(Ohlc Bar, DateTimeOffset Start, DateOnly? ExchangeLocalDate);

    #endregion
}
