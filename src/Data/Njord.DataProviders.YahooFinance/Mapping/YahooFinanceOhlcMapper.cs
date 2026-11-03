using System.Globalization;
using Njord.Core.Mapping;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Errors;
using Njord.DataProviders.YahooFinance.Serialization;
using Njord.Models.MarketData;

namespace Njord.DataProviders.YahooFinance.Mapping;

internal sealed class YahooFinanceOhlcMapper : IMapper<YahooFinanceOhlcMappingInput, YahooFinanceOhlcMappingResult>
{
    #region Market data mapping

    public YahooFinanceOhlcMappingResult Map(YahooFinanceOhlcMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Chart);
        ArgumentNullException.ThrowIfNull(input.Request);

        var chart = input.Chart;
        var request = input.Request;
        var metadata = chart.Meta;
        if (metadata is null || string.IsNullOrWhiteSpace(metadata.Symbol))
            throw InvalidChart("Yahoo Finance returned chart data without a symbol.");

        if (!string.Equals(metadata.Symbol, request.Ticker, StringComparison.OrdinalIgnoreCase))
            throw InvalidChart("Yahoo Finance returned chart data for a different ticker.");

        var timestamps = chart.Timestamps;
        var quote = chart.Indicators?.Quote is { Count: 1 } quotes
            ? quotes[0]
            : null;
        if (timestamps is null || quote is null)
            throw InvalidChart("Yahoo Finance returned chart data without timestamps or OHLC indicators.");

        if (timestamps.Count > input.MaximumBarsPerQuery)
            throw InvalidChart(
                $"Yahoo Finance returned more than the configured maximum of {input.MaximumBarsPerQuery} bars.");

        ValidateFieldLength(quote.Open, timestamps.Count, "open");
        ValidateFieldLength(quote.High, timestamps.Count, "high");
        ValidateFieldLength(quote.Low, timestamps.Count, "low");
        ValidateFieldLength(quote.Close, timestamps.Count, "close");

        var bars = new List<Ohlc>(timestamps.Count);
        var diagnosticGroups = new Dictionary<string, BarDiagnosticGroup>(StringComparer.Ordinal);
        for (var index = 0; index < timestamps.Count; index++)
        {
            var bar = MapBar(
                quote,
                index,
                timestamps[index],
                request,
                metadata.Currency,
                diagnosticGroups);
            if (bar is not null)
                bars.Add(bar);
        }

        var diagnostics = diagnosticGroups
            .Select(item => new YahooFinanceDataDiagnostic
            {
                Code = item.Key,
                Message = item.Value.Message,
                AffectedBarCount = item.Value.Count,
                FirstAffectedTimestamp = item.Value.FirstTimestamp
            })
            .ToArray();

        return new YahooFinanceOhlcMappingResult
        {
            Bars = Array.AsReadOnly(bars.OrderBy(bar => bar.Timestamp).ToArray()),
            Diagnostics = Array.AsReadOnly(diagnostics)
        };
    }

    private static Ohlc? MapBar(
        YahooFinanceQuoteDto quote,
        int index,
        long unixTimestamp,
        YahooFinanceResolvedOhlcRequest request,
        string? currency,
        IDictionary<string, BarDiagnosticGroup> diagnosticGroups)
    {
        DateTimeOffset timestamp;
        try
        {
            timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            AddBarDiagnostic(
                diagnosticGroups,
                "InvalidTimestamp",
                "Yahoo Finance returned an unsupported timestamp; the affected bar was skipped.",
                null);
            return null;
        }

        var from = new DateTimeOffset(request.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toExclusive = new DateTimeOffset(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if (timestamp < from || timestamp >= toExclusive)
        {
            AddBarDiagnostic(
                diagnosticGroups,
                "OutsideRequestedRange",
                "Yahoo Finance returned bars outside the requested UTC date range; they were skipped.",
                timestamp);
            return null;
        }

        var open = quote.Open![index];
        var high = quote.High![index];
        var low = quote.Low![index];
        var close = quote.Close![index];
        if (!open.HasValue || !high.HasValue || !low.HasValue || !close.HasValue)
        {
            AddBarDiagnostic(
                diagnosticGroups,
                "MissingOhlc",
                "Yahoo Finance omitted one or more OHLC prices; affected bars were skipped.",
                timestamp);
            return null;
        }

        if (high < low || high < open || high < close || low > open || low > close)
        {
            AddBarDiagnostic(
                diagnosticGroups,
                "InvalidOhlc",
                "Yahoo Finance returned inconsistent OHLC prices; affected bars were skipped.",
                timestamp);
            return null;
        }

        var volume = quote.Volume is not null && index < quote.Volume.Count
            ? quote.Volume[index]
            : null;
        if (quote.Volume is not null && index >= quote.Volume.Count)
            AddBarDiagnostic(
                diagnosticGroups,
                "MissingVolume",
                "Yahoo Finance omitted volume values; affected bars have no volume.",
                timestamp);

        if (volume < 0)
        {
            AddBarDiagnostic(
                diagnosticGroups,
                "InvalidVolume",
                "Yahoo Finance returned negative volume; affected bars have no volume.",
                timestamp);
            volume = null;
        }

        return new Ohlc
        {
            Ticker = request.NjordTicker,
            Open = open.Value,
            High = high.Value,
            Low = low.Value,
            Close = close.Value,
            Timestamp = timestamp,
            PeriodStart = timestamp,
            Interval = request.ReportedInterval,
            Currency = string.IsNullOrWhiteSpace(currency) ? null : currency,
            Volume = volume,
            VolumeWeightedAveragePrice = null,
            TransactionCount = null,
            IsAdjusted = null
        };
    }

    private static void ValidateFieldLength(
        IReadOnlyCollection<decimal?>? values,
        int timestampCount,
        string fieldName)
    {
        if (values is null || values.Count != timestampCount)
            throw InvalidChart(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Yahoo Finance returned a missing or incomplete '{fieldName}' series."));
    }

    private static YahooFinanceDataProviderException InvalidChart(string message) => new(message);

    private static void AddBarDiagnostic(
        IDictionary<string, BarDiagnosticGroup> diagnostics,
        string code,
        string message,
        DateTimeOffset? timestamp)
    {
        if (diagnostics.TryGetValue(code, out var group))
        {
            diagnostics[code] = group with
            {
                Count = group.Count + 1,
                FirstTimestamp = group.FirstTimestamp ?? timestamp
            };
            return;
        }

        diagnostics.Add(code, new BarDiagnosticGroup(message, 1, timestamp));
    }

    private sealed record BarDiagnosticGroup(string Message, int Count, DateTimeOffset? FirstTimestamp);

    #endregion
}
