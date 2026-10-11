using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;
using Njord.DataProviders.Massive.Serialization;
using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Mapping;

internal sealed class MassiveOhlcMapper : IMassiveOhlcMapper
{
    #region Market data mapping

    public IReadOnlyList<Ohlc> Map(
        IReadOnlyList<MassiveAggregateResponseDto> pages,
        MassiveHistoricalOhlcRequest request)
    {
        var bars = new List<Ohlc>();
        foreach (var page in pages)
        {
            if (page.Results is null)
                throw new MassiveDataProviderException(
                    "Massive returned a response without a results array.",
                    requestId: page.RequestId);

            foreach (var aggregate in page.Results)
            {
                if (aggregate.Ticker is not null &&
                    !string.Equals(aggregate.Ticker, request.Ticker, StringComparison.Ordinal))
                    throw InvalidAggregate("Massive returned a futures bar for a different ticker.", page.RequestId);

                bars.Add(MapBar(aggregate, page.Adjusted, request, page.RequestId));
            }
        }

        return bars.AsReadOnly();
    }

    private static Ohlc MapBar(
        MassiveAggregateDto aggregate,
        bool? isAdjusted,
        MassiveHistoricalOhlcRequest request,
        string? requestId)
    {
        var open = aggregate.Open ?? aggregate.FuturesOpen;
        var high = aggregate.High ?? aggregate.FuturesHigh;
        var low = aggregate.Low ?? aggregate.FuturesLow;
        var close = aggregate.Close ?? aggregate.FuturesClose;
        var volume = aggregate.Volume ?? aggregate.FuturesVolume;
        var transactions = aggregate.Transactions ?? aggregate.FuturesTransactions;

        if (!open.HasValue || !high.HasValue || !low.HasValue || !close.HasValue)
            throw InvalidAggregate("An aggregate is missing one or more OHLC prices.", requestId);

        if (high < low || high < open || high < close || low > open || low > close)
            throw InvalidAggregate("Massive returned inconsistent OHLC price values.", requestId);

        if (volume < 0 || transactions < 0 ||
            aggregate.VolumeWeightedAveragePrice < 0 ||
            aggregate.VolumeWeightedAveragePrice < low ||
            aggregate.VolumeWeightedAveragePrice > high)
            throw InvalidAggregate("Massive returned an invalid volume, transaction count, or VWAP.", requestId);

        var timestamp = GetTimestamp(aggregate, request.AssetClass, requestId);
        return new Ohlc
        {
            Ticker = request.Ticker,
            Open = open.Value,
            High = high.Value,
            Low = low.Value,
            Close = close.Value,
            Timestamp = timestamp,
            PeriodStart = timestamp,
            Interval = GetInterval(request),
            Currency = null,
            Volume = volume,
            VolumeWeightedAveragePrice = aggregate.VolumeWeightedAveragePrice,
            TransactionCount = transactions,
            IsAdjusted = isAdjusted
        };
    }

    private static DateTimeOffset GetTimestamp(
        MassiveAggregateDto aggregate,
        MassiveAssetClass assetClass,
        string? requestId)
    {
        try
        {
            if (assetClass == MassiveAssetClass.Future)
            {
                if (!aggregate.WindowStartNanoseconds.HasValue)
                    throw InvalidAggregate("A futures aggregate is missing its window_start timestamp.", requestId);

                return DateTimeOffset.UnixEpoch.AddTicks(aggregate.WindowStartNanoseconds.Value / 100);
            }

            if (!aggregate.TimestampMilliseconds.HasValue)
                throw InvalidAggregate("An aggregate is missing its timestamp.", requestId);

            return DateTimeOffset.FromUnixTimeMilliseconds(aggregate.TimestampMilliseconds.Value);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new MassiveDataProviderException(
                "Massive returned an aggregate timestamp outside the supported range.",
                requestId: requestId,
                innerException: exception);
        }
    }

    private static TimeSpan? GetInterval(MassiveHistoricalOhlcRequest request)
    {
        var unit = request.Timespan switch
        {
            MassiveTimespan.Second => TimeSpan.TicksPerSecond,
            MassiveTimespan.Minute => TimeSpan.TicksPerMinute,
            MassiveTimespan.Hour => TimeSpan.TicksPerHour,
            MassiveTimespan.Day => TimeSpan.TicksPerDay,
            _ => 0
        };

        if (unit == 0)
            return null;

        if (request.Multiplier > TimeSpan.MaxValue.Ticks / unit)
            throw new ArgumentOutOfRangeException(nameof(request), "The requested interval cannot be represented as a TimeSpan.");

        return TimeSpan.FromTicks(unit * request.Multiplier);
    }

    private static MassiveDataProviderException InvalidAggregate(string message, string? requestId) =>
        new(message, requestId: requestId);

    #endregion
}
