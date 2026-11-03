using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Njord.Core.Mapping;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.Data.MarketData.Contracts;
using Njord.DataProviders.YahooFinance.Configuration;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Errors;
using Njord.DataProviders.YahooFinance.Mapping;
using Njord.DataProviders.YahooFinance.Serialization;
using Njord.DataProviders.YahooFinance.Transport;
using Njord.Models.MarketData;

namespace Njord.DataProviders.YahooFinance.Provider;

internal sealed class YahooFinanceHistoricalMarketDataProvider :
    IYahooFinanceHistoricalMarketDataProvider,
    IMarketDataProvider,
    IMarketDataProvider<IMarketData>,
    ICorporateActionProvider
{
    #region Provider dependencies

    private readonly IYahooFinanceChartApiClient _apiClient;
    private readonly IMapper<YahooFinanceOhlcMappingInput, YahooFinanceOhlcMappingResult> _ohlcMapper;
    private readonly IMapper<YahooFinanceHistoricalOhlcRequest, YahooFinanceIntervalMapping> _intervalMapper;
    private readonly IInstrumentReferential _referential;
    private readonly YahooFinanceDataProviderOptions _options;
    private readonly ILogger<YahooFinanceHistoricalMarketDataProvider> _logger;

    public YahooFinanceHistoricalMarketDataProvider(
        IYahooFinanceChartApiClient apiClient,
        IMapper<YahooFinanceOhlcMappingInput, YahooFinanceOhlcMappingResult> ohlcMapper,
        IMapper<YahooFinanceHistoricalOhlcRequest, YahooFinanceIntervalMapping> intervalMapper,
        IInstrumentReferential referential,
        IOptions<YahooFinanceDataProviderOptions> options,
        ILogger<YahooFinanceHistoricalMarketDataProvider> logger)
    {
        _apiClient = apiClient;
        _ohlcMapper = ohlcMapper;
        _intervalMapper = intervalMapper;
        _referential = referential;
        _options = options.Value;
        _logger = logger;
    }

    #endregion

    #region Historical market data

    public async Task<IReadOnlyList<YahooFinanceTickerOhlcResult>> GetHistoricalOhlcAsync(
        YahooFinanceHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var interval = _intervalMapper.Map(request);
        var started = Stopwatch.GetTimestamp();

        var results = new List<YahooFinanceTickerOhlcResult>(request.Tickers.Count);
        foreach (var ticker in request.Tickers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tickerBars = new List<Ohlc>();
            var diagnostics = new List<YahooFinanceDataDiagnostic>();
            IReadOnlyList<TickerProviderMapping> mappings;
            try
            {
                mappings = await _referential
                    .ResolveAsync(ticker, "YahooFinance", request.From, request.To, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TickerReferenceResolutionException exception)
            {
                diagnostics.Add(new YahooFinanceDataDiagnostic
                {
                    Code = "TickerMappingError",
                    Message = exception.Message
                });
                results.Add(CreateTickerResult(ticker, tickerBars, diagnostics));
                continue;
            }

            string? exchangeTimeZoneId = null;
            var hasExchangeTimeZoneConflict = false;
            foreach (var mapping in mappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var resolvedRequest = CreateResolvedRequest(ticker, mapping, request, interval);
                    var chart = await _apiClient
                        .GetHistoricalChartAsync(resolvedRequest, cancellationToken)
                        .ConfigureAwait(false);
                    var reportedTimeZone = chart.Meta?.ExchangeTimeZoneName;
                    if (!string.IsNullOrWhiteSpace(reportedTimeZone))
                    {
                        if (exchangeTimeZoneId is null)
                            exchangeTimeZoneId = reportedTimeZone;
                        else if (!string.Equals(
                            exchangeTimeZoneId,
                            reportedTimeZone,
                            StringComparison.Ordinal))
                        {
                            hasExchangeTimeZoneConflict = true;
                        }
                    }

                    var mappedResult = _ohlcMapper.Map(new YahooFinanceOhlcMappingInput
                    {
                        Chart = chart,
                        Request = resolvedRequest,
                        MaximumBarsPerQuery = _options.MaximumBarsPerQuery
                    });
                    tickerBars.AddRange(mappedResult.Bars);
                    diagnostics.AddRange(mappedResult.Diagnostics);
                }
                catch (YahooFinanceDataProviderException exception)
                {
                    diagnostics.Add(CreateProviderDiagnostic(exception));
                }
            }

            if (hasExchangeTimeZoneConflict)
            {
                exchangeTimeZoneId = null;
                diagnostics.Add(new YahooFinanceDataDiagnostic
                {
                    Code = "ExchangeTimeZoneConflict",
                    Message = "Yahoo Finance reported different exchange timezones for mappings of this ticker."
                });
            }
            else if (exchangeTimeZoneId is null && tickerBars.Count > 0)
            {
                diagnostics.Add(new YahooFinanceDataDiagnostic
                {
                    Code = "MissingExchangeTimeZone",
                    Message = "Yahoo Finance did not report an exchange timezone for this ticker."
                });
            }

            if (tickerBars.Count == 0 && diagnostics.Count == 0)
                diagnostics.Add(new YahooFinanceDataDiagnostic
                {
                    Code = "NoData",
                    Message = "Yahoo Finance returned no OHLC bars for this ticker and date range."
                });

            results.Add(CreateTickerResult(ticker, tickerBars, diagnostics, exchangeTimeZoneId));
        }

        var tickerResults = Array.AsReadOnly(results.ToArray());
        var barCount = tickerResults.Sum(result => result.Bars.Count);
        var diagnosticCount = tickerResults.Sum(result => result.Diagnostics.Count);
        _logger.LogInformation(
            "Retrieved Yahoo Finance historical OHLC data for {TickerCount} tickers: {BarCount} bars and {DiagnosticCount} diagnostics in {ElapsedMilliseconds} ms.",
            request.Tickers.Count,
            barCount,
            diagnosticCount,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        return tickerResults;
    }

    private static YahooFinanceTickerOhlcResult CreateTickerResult(
        string ticker,
        IReadOnlyCollection<Ohlc> bars,
        IReadOnlyCollection<YahooFinanceDataDiagnostic> diagnostics,
        string? exchangeTimeZoneId = null) =>
        new()
        {
            Ticker = ticker,
            ExchangeTimeZoneId = exchangeTimeZoneId,
            Bars = bars.OrderBy(bar => bar.Timestamp).ToArray(),
            Diagnostics = diagnostics.ToArray()
        };

    private static YahooFinanceDataDiagnostic CreateProviderDiagnostic(
        YahooFinanceDataProviderException exception) =>
        new()
        {
            Code = exception.ErrorCode ??
                (exception.StatusCode.HasValue ? "HttpError" : "ProviderError"),
            Message = exception.Message,
            StatusCode = exception.StatusCode
        };

    #endregion

    #region Common market-data contract

    public string ProviderName => "YahooFinance";

    public async Task<IReadOnlyList<IMarketDataResult<TMarketData>>> Get<TMarketData>(
        IMarketDataRequest<TMarketData> request,
        CancellationToken cancellationToken = default)
        where TMarketData : IMarketData
    {
        ArgumentNullException.ThrowIfNull(request);

        if (typeof(TMarketData) == typeof(Ohlc) &&
            request is MarketDataHistoricalOhlcRequest ohlcRequest)
        {
            var results = await GetHistoricalOhlcAsync(ohlcRequest, cancellationToken).ConfigureAwait(false);
            return Array.AsReadOnly(
                results.Select(result =>
                    result is IMarketDataResult<TMarketData> typedResult
                        ? typedResult
                        : throw new InvalidOperationException(
                            "Yahoo Finance returned an OHLC result incompatible with the requested market-data type."))
                    .ToArray());
        }

        if (typeof(TMarketData) == typeof(MarketCorporateAction) &&
            request is CorporateActionHistoricalRequest corporateActionRequest)
        {
            var results = await GetCorporateActionsAsync(corporateActionRequest, cancellationToken)
                .ConfigureAwait(false);
            return Array.AsReadOnly(
                results.Select(result =>
                    result is IMarketDataResult<TMarketData> typedResult
                        ? typedResult
                        : throw new InvalidOperationException(
                            "Yahoo Finance returned a corporate-action result incompatible with the requested market-data type."))
                    .ToArray());
        }

        throw new NotSupportedException(
            $"Yahoo Finance does not support request '{request.GetType().Name}' for market-data type '{typeof(TMarketData).Name}'.");
    }

    public async Task<IReadOnlyList<MarketDataTickerOhlcResult>> GetHistoricalOhlcAsync(
        MarketDataHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var yahooRequest = new YahooFinanceHistoricalOhlcRequest
        {
            Tickers = request.Tickers,
            From = request.From,
            To = request.To,
            Multiplier = request.Multiplier,
            Timespan = MapInterval(request.Interval)
        };
        var results = await GetHistoricalOhlcAsync(yahooRequest, cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(
            results.Select(result => new MarketDataTickerOhlcResult
            {
                Ticker = result.Ticker,
                Bars = result.Bars,
                Diagnostics = result.Diagnostics.Select(MapDiagnostic).ToArray(),
                ExchangeTimeZoneId = result.ExchangeTimeZoneId
            }).ToArray());
    }

    private static YahooFinanceTimespan MapInterval(MarketDataInterval interval) =>
        interval switch
        {
            MarketDataInterval.Minute => YahooFinanceTimespan.Minute,
            MarketDataInterval.Hour => YahooFinanceTimespan.Hour,
            MarketDataInterval.Day => YahooFinanceTimespan.Day,
            MarketDataInterval.Week => YahooFinanceTimespan.Week,
            MarketDataInterval.Month => YahooFinanceTimespan.Month,
            MarketDataInterval.Quarter => YahooFinanceTimespan.Quarter,
            _ => throw new YahooFinanceDataProviderException(
                $"Yahoo Finance does not support the requested {interval} interval.")
        };

    private static MarketDataDiagnostic MapDiagnostic(YahooFinanceDataDiagnostic diagnostic) =>
        new()
        {
            Code = diagnostic.Code,
            Message = diagnostic.Message,
            StatusCode = diagnostic.StatusCode,
            AffectedCount = diagnostic.AffectedBarCount,
            FirstAffectedTimestamp = diagnostic.FirstAffectedTimestamp
        };

    #endregion

    #region Corporate actions

    public async Task<IReadOnlyList<MarketDataTickerCorporateActionResult>> GetCorporateActionsAsync(
        CorporateActionHistoricalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var interval = new YahooFinanceIntervalMapping
        {
            YahooInterval = "1d",
            ReportedInterval = TimeSpan.FromDays(1)
        };
        var dailyRequest = new YahooFinanceHistoricalOhlcRequest
        {
            Tickers = request.Tickers,
            From = request.From,
            To = request.To,
            Multiplier = 1,
            Timespan = YahooFinanceTimespan.Day
        };
        var results = new List<MarketDataTickerCorporateActionResult>(request.Tickers.Count);

        foreach (var ticker in request.Tickers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actions = new List<MarketCorporateAction>();
            var diagnostics = new List<MarketDataDiagnostic>
            {
                new()
                {
                    Code = "UnsupportedActionTypes",
                    Message = "Yahoo Finance chart events expose splits and cash dividends only; other corporate-action types are unavailable through this endpoint."
                }
            };

            IReadOnlyList<TickerProviderMapping> mappings;
            try
            {
                mappings = await _referential
                    .ResolveAsync(ticker, "YahooFinance", request.From, request.To, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TickerReferenceResolutionException exception)
            {
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "TickerMappingError",
                    Message = exception.Message
                });
                results.Add(CreateCorporateActionResult(ticker, actions, diagnostics));
                continue;
            }

            if (mappings.Count == 0)
            {
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "NoTickerMapping",
                    Message = $"No Yahoo Finance ticker mapping covers '{ticker}' and the requested date range."
                });
                results.Add(CreateCorporateActionResult(ticker, actions, diagnostics));
                continue;
            }

            var hasSuccessfulResponse = false;
            foreach (var mapping in mappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var resolvedRequest = CreateResolvedRequest(ticker, mapping, dailyRequest, interval);
                    var chart = await _apiClient
                        .GetHistoricalChartAsync(
                            resolvedRequest,
                            cancellationToken,
                            includeCorporateActions: true)
                        .ConfigureAwait(false);
                    MapCorporateActions(chart, resolvedRequest, actions, diagnostics);
                    hasSuccessfulResponse = true;
                }
                catch (YahooFinanceDataProviderException exception)
                {
                    diagnostics.Add(MapDiagnostic(CreateProviderDiagnostic(exception)));
                }
            }

            if (actions.Count == 0 && hasSuccessfulResponse)
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "NoCorporateActions",
                    Message = "Yahoo Finance returned no split or dividend events for this ticker and date range."
                });

            results.Add(CreateCorporateActionResult(ticker, actions, diagnostics));
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static void MapCorporateActions(
        YahooFinanceChartResultDto chart,
        YahooFinanceResolvedOhlcRequest request,
        ICollection<MarketCorporateAction> actions,
        ICollection<MarketDataDiagnostic> diagnostics)
    {
        var metadata = chart.Meta;
        if (metadata is null ||
            !string.Equals(metadata.Symbol, request.Ticker, StringComparison.OrdinalIgnoreCase))
            throw new YahooFinanceDataProviderException(
                "Yahoo Finance returned corporate actions without matching ticker metadata.");

        if (chart.Events is null)
            return;

        foreach (var (eventId, dividend) in chart.Events.Dividends?.AsEnumerable() ??
                 Enumerable.Empty<KeyValuePair<string, YahooFinanceDividendEventDto>>())
        {
            if (!TryGetActionDate(dividend.Date, metadata.ExchangeTimeZoneName, out var exDividendDate))
            {
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "InvalidDividendEvent",
                    Message = "Yahoo Finance returned a dividend without a valid exchange-local event date."
                });
                continue;
            }

            if (!dividend.Amount.HasValue)
            {
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "InvalidDividendEvent",
                    Message = "Yahoo Finance returned a dividend without an amount."
                });
                continue;
            }

            if (exDividendDate < request.From || exDividendDate > request.To)
                continue;

            actions.Add(new MarketCorporateAction
            {
                Kind = CorporateActionKind.Dividend,
                ExDividendDate = exDividendDate,
                DividendAmount = dividend.Amount,
                EventId = eventId
            });
        }

        foreach (var (eventId, split) in chart.Events.Splits?.AsEnumerable() ??
                 Enumerable.Empty<KeyValuePair<string, YahooFinanceSplitEventDto>>())
        {
            if (!TryGetActionDate(split.Date, metadata.ExchangeTimeZoneName, out var executionDate) ||
                !split.Numerator.HasValue ||
                !split.Denominator.HasValue ||
                split.Numerator <= 0 ||
                split.Denominator <= 0)
            {
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "InvalidSplitEvent",
                    Message = "Yahoo Finance returned a split without a valid date and positive ratio."
                });
                continue;
            }

            if (executionDate < request.From || executionDate > request.To)
                continue;

            actions.Add(new MarketCorporateAction
            {
                Kind = CorporateActionKind.Split,
                ExecutionDate = executionDate,
                SplitFrom = split.Denominator,
                SplitTo = split.Numerator,
                SplitRatio = split.SplitRatio,
                EventId = eventId
            });
        }
    }

    private static bool TryGetActionDate(
        long? unixTimestamp,
        string? exchangeTimeZoneId,
        out DateOnly actionDate)
    {
        actionDate = default;
        if (!unixTimestamp.HasValue || string.IsNullOrWhiteSpace(exchangeTimeZoneId))
            return false;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(exchangeTimeZoneId);
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value);
            actionDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timestamp, timeZone).DateTime);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static MarketDataTickerCorporateActionResult CreateCorporateActionResult(
        string ticker,
        IEnumerable<MarketCorporateAction> actions,
        IEnumerable<MarketDataDiagnostic> diagnostics) =>
        new()
        {
            Ticker = ticker,
            Actions = actions
                .OrderBy(action => action.EffectiveDate ?? action.ExecutionDate ?? action.ExDividendDate)
                .ToArray(),
            Diagnostics = diagnostics.ToArray()
        };

    #endregion

    #region Referential mapping

    private static YahooFinanceResolvedOhlcRequest CreateResolvedRequest(
        string njordTicker,
        TickerProviderMapping mapping,
        YahooFinanceHistoricalOhlcRequest request,
        YahooFinanceIntervalMapping interval)
    {
        if (mapping.AssetClass is not (
            NjordAssetClass.Stock or
            NjordAssetClass.Index or
            NjordAssetClass.Option or
            NjordAssetClass.Future or
            NjordAssetClass.Crypto or
            NjordAssetClass.Forex))
            throw new YahooFinanceDataProviderException(
                $"Yahoo Finance does not support Njord asset class '{mapping.AssetClass}' for ticker '{njordTicker}'.");

        var mappingStart = DateOnly.FromDateTime(mapping.StartDate);
        var mappingEnd = DateOnly.FromDateTime(mapping.EndDate);
        var from = mappingStart > request.From ? mappingStart : request.From;
        var to = mappingEnd < request.To ? mappingEnd : request.To;

        return new YahooFinanceResolvedOhlcRequest
        {
            NjordTicker = mapping.Ticker,
            Ticker = mapping.ProviderTicker,
            From = from,
            To = to,
            Interval = interval.YahooInterval,
            ReportedInterval = interval.ReportedInterval
        };
    }

    #endregion
}
