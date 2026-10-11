using System.Globalization;
using System.Text.Json;
using Njord.DataProviders.YahooFinance.Serialization;
using Njord.DataProviders.YahooFinance.Transport;
using Njord.MarketData;
using static Njord.DataProviders.YahooFinance.Serialization.YahooFinanceJsonReader;

namespace Njord.DataProviders.YahooFinance.Chart;

internal sealed class YahooFinanceChartDataSource(YahooFinanceHttpClient httpClient)
{
    private static readonly IReadOnlyDictionary<MarketDataInterval, string> YahooIntervals =
        new Dictionary<MarketDataInterval, string>
        {
            [MarketDataInterval.OneMinute] = "1m",
            [MarketDataInterval.TwoMinutes] = "2m",
            [MarketDataInterval.FiveMinutes] = "5m",
            [MarketDataInterval.FifteenMinutes] = "15m",
            [MarketDataInterval.ThirtyMinutes] = "30m",
            [MarketDataInterval.SixtyMinutes] = "60m",
            [MarketDataInterval.NinetyMinutes] = "90m",
            [MarketDataInterval.OneHour] = "1h",
            [MarketDataInterval.OneDay] = "1d",
            [MarketDataInterval.FiveDays] = "5d",
            [MarketDataInterval.OneWeek] = "1wk",
            [MarketDataInterval.OneMonth] = "1mo",
            [MarketDataInterval.ThreeMonths] = "3mo"
        };

    public async Task<IReadOnlyList<MarketDataBar>> GetHistoricalBarsAsync(
        HistoricalMarketDataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureSupported(request.Instrument);

        var query =
            $"period1={request.Start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}" +
            $"&period2={request.End.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}" +
            $"&interval={YahooIntervals[request.Interval]}";
        var chart = await httpClient.GetChartResponseAsync(
            request.Instrument.Symbol,
            query,
            cancellationToken).ConfigureAwait(false);
        ThrowIfApiError(chart);
        var result = GetSingleResult(chart);
        var metadata = ReadMetadata(result);
        var timestamps = ReadTimestamps(result);
        if (timestamps.Length == 0)
        {
            return Array.Empty<MarketDataBar>();
        }

        var indicators = GetRequiredProperty(result, "indicators");
        var quoteArray = GetRequiredProperty(indicators, "quote");
        if (quoteArray.ValueKind != JsonValueKind.Array || quoteArray.GetArrayLength() != 1)
        {
            throw InvalidResponse("Yahoo Finance returned an invalid quote series.");
        }

        var quote = quoteArray[0];
        var opens = ReadSeries(quote, "open", timestamps.Length, required: true)!;
        var highs = ReadSeries(quote, "high", timestamps.Length, required: true)!;
        var lows = ReadSeries(quote, "low", timestamps.Length, required: true)!;
        var closes = ReadSeries(quote, "close", timestamps.Length, required: true)!;
        var volumes = ReadSeries(quote, "volume", timestamps.Length, required: false);
        var adjustedCloses = ReadAdjustedCloseSeries(indicators, timestamps.Length);
        var instrument = CreateInstrument(request.Instrument, metadata.Exchange);

        var bars = new List<MarketDataBar>(timestamps.Length);
        for (var index = 0; index < timestamps.Length; index++)
        {
            if (opens[index] is not { } open ||
                highs[index] is not { } high ||
                lows[index] is not { } low ||
                closes[index] is not { } close)
            {
                continue;
            }

            try
            {
                bars.Add(new MarketDataBar(
                    instrument,
                    ParseTimestamp(timestamps[index]),
                    request.Interval,
                    open,
                    high,
                    low,
                    close,
                    volumes?[index],
                    metadata.Currency,
                    "YahooFinance",
                    adjustedCloses?[index]));
            }
            catch (ArgumentException exception)
            {
                throw InvalidResponse($"Yahoo Finance returned an invalid OHLCV bar at index {index}.", exception);
            }
        }

        bars.Sort(static (left, right) => left.Timestamp.CompareTo(right.Timestamp));
        for (var index = 1; index < bars.Count; index++)
        {
            if (bars[index - 1].Timestamp == bars[index].Timestamp)
            {
                throw InvalidResponse("Yahoo Finance returned duplicate bar timestamps.");
            }
        }

        return bars;
    }

    public async Task<MarketDataQuote> GetQuoteAsync(
        MarketInstrument instrument,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        EnsureSupported(instrument);

        var chart = await httpClient.GetChartResponseAsync(
            instrument.Symbol,
            "range=5d&interval=1d",
            cancellationToken).ConfigureAwait(false);
        ThrowIfApiError(chart);
        var result = GetSingleResult(chart);
        var metadata = ReadMetadata(result);
        var timestamps = ReadTimestamps(result);
        var quoteMetadata = GetRequiredProperty(result, "meta");
        var marketPrice = ReadOptionalDecimal(quoteMetadata, "regularMarketPrice");
        DateTimeOffset? marketTimestamp = ReadOptionalInt64(quoteMetadata, "regularMarketTime") is { } marketTime
            ? ParseTimestamp(marketTime)
            : null;

        decimal lastPrice;
        DateTimeOffset timestamp;
        if (marketPrice is { } availablePrice && marketTimestamp is { } availableTimestamp)
        {
            lastPrice = availablePrice;
            timestamp = availableTimestamp;
        }
        else
        {
            var lastClose = ReadLastClose(result, timestamps);
            if (lastClose.Price is not { } close || lastClose.Timestamp is not { } closeTimestamp)
            {
                throw InvalidResponse("Yahoo Finance did not return a usable quote price and timestamp.");
            }

            lastPrice = close;
            timestamp = closeTimestamp;
        }

        return new MarketDataQuote(
            CreateInstrument(instrument, metadata.Exchange),
            timestamp,
            lastPrice,
            metadata.Currency,
            "YahooFinance",
            ReadOptionalDecimal(quoteMetadata, "bid"),
            ReadOptionalDecimal(quoteMetadata, "ask"),
            ReadOptionalDecimal(quoteMetadata, "chartPreviousClose")
                ?? ReadOptionalDecimal(quoteMetadata, "previousClose"));
    }

    private static void EnsureSupported(MarketInstrument instrument)
    {
        if (instrument.AssetClass is not (
            MarketAssetClass.Equity or
            MarketAssetClass.Index or
            MarketAssetClass.Future or
            MarketAssetClass.Option or
            MarketAssetClass.ForeignExchange))
        {
            throw new NotSupportedException(
                $"Yahoo Finance provider does not support {instrument.AssetClass} instruments.");
        }
    }

    private static JsonElement GetSingleResult(JsonElement chart)
    {
        var results = GetRequiredProperty(chart, "result");
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() != 1)
        {
            throw InvalidResponse("Yahoo Finance did not return exactly one chart result.");
        }

        return results[0];
    }

    private static ChartMetadata ReadMetadata(JsonElement result)
    {
        var metadata = GetRequiredProperty(result, "meta");
        var currency = ReadOptionalString(metadata, "currency");
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw InvalidResponse("Yahoo Finance response did not include an instrument currency.");
        }

        var exchange = ReadOptionalString(metadata, "fullExchangeName")
            ?? ReadOptionalString(metadata, "exchangeName");
        return new ChartMetadata(currency, exchange);
    }

    private static MarketInstrument CreateInstrument(MarketInstrument instrument, string? providerExchange) =>
        new(instrument.Symbol.Trim().ToUpperInvariant(), instrument.AssetClass, instrument.Exchange ?? providerExchange);

    private static long[] ReadTimestamps(JsonElement result)
    {
        var timestamps = GetRequiredProperty(result, "timestamp");
        if (timestamps.ValueKind != JsonValueKind.Array)
        {
            throw InvalidResponse("Yahoo Finance response did not contain a valid timestamp array.");
        }

        var values = new long[timestamps.GetArrayLength()];
        for (var index = 0; index < values.Length; index++)
        {
            if (!timestamps[index].TryGetInt64(out values[index]))
            {
                throw InvalidResponse($"Yahoo Finance returned an invalid timestamp at index {index}.");
            }
        }

        return values;
    }

    private static decimal?[]? ReadAdjustedCloseSeries(JsonElement indicators, int expectedLength)
    {
        if (indicators.ValueKind != JsonValueKind.Object)
        {
            throw InvalidResponse("Yahoo Finance returned invalid indicator data.");
        }

        if (!indicators.TryGetProperty("adjclose", out var adjustedClose) ||
            adjustedClose.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (adjustedClose.ValueKind != JsonValueKind.Array || adjustedClose.GetArrayLength() != 1)
        {
            throw InvalidResponse("Yahoo Finance returned an invalid adjusted-close series.");
        }

        return ReadSeries(adjustedClose[0], "adjclose", expectedLength, required: true);
    }

    private static decimal?[]? ReadSeries(JsonElement quote, string name, int expectedLength, bool required)
    {
        if (quote.ValueKind != JsonValueKind.Object)
        {
            throw InvalidResponse($"Yahoo Finance returned invalid {name} series data.");
        }

        if (!quote.TryGetProperty(name, out var values) || values.ValueKind == JsonValueKind.Null)
        {
            if (required)
            {
                throw InvalidResponse($"Yahoo Finance response did not include the {name} series.");
            }

            return null;
        }

        if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength() != expectedLength)
        {
            throw InvalidResponse($"Yahoo Finance returned an invalid {name} series.");
        }

        var series = new decimal?[expectedLength];
        for (var index = 0; index < series.Length; index++)
        {
            var value = values[index];
            if (value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var parsedValue))
            {
                throw InvalidResponse($"Yahoo Finance returned an invalid {name} value at index {index}.");
            }

            series[index] = parsedValue;
        }

        return series;
    }

    private static (decimal? Price, DateTimeOffset? Timestamp) ReadLastClose(
        JsonElement result,
        long[] timestamps)
    {
        if (timestamps.Length == 0)
        {
            return (null, null);
        }

        var indicators = GetRequiredProperty(result, "indicators");
        var quoteArray = GetRequiredProperty(indicators, "quote");
        if (quoteArray.ValueKind != JsonValueKind.Array || quoteArray.GetArrayLength() != 1)
        {
            throw InvalidResponse("Yahoo Finance returned an invalid quote series.");
        }

        var closes = ReadSeries(quoteArray[0], "close", timestamps.Length, required: true)!;
        for (var index = timestamps.Length - 1; index >= 0; index--)
        {
            if (closes[index] is { } close)
            {
                return (close, ParseTimestamp(timestamps[index]));
            }
        }

        return (null, null);
    }

    private sealed record ChartMetadata(string Currency, string? Exchange);
}
