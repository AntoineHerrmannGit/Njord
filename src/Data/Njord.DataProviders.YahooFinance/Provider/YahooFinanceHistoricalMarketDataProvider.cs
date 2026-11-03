using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Njord.Core.Mapping;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.DataProviders.YahooFinance.Configuration;
using Njord.DataProviders.YahooFinance.Contracts;
using Njord.DataProviders.YahooFinance.Errors;
using Njord.DataProviders.YahooFinance.Mapping;
using Njord.DataProviders.YahooFinance.Transport;
using Njord.Models.MarketData;

namespace Njord.DataProviders.YahooFinance.Provider;

internal sealed class YahooFinanceHistoricalMarketDataProvider : IYahooFinanceHistoricalMarketDataProvider
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

            foreach (var mapping in mappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var resolvedRequest = CreateResolvedRequest(ticker, mapping, request, interval);
                    var chart = await _apiClient
                        .GetHistoricalChartAsync(resolvedRequest, cancellationToken)
                        .ConfigureAwait(false);
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

            if (tickerBars.Count == 0 && diagnostics.Count == 0)
                diagnostics.Add(new YahooFinanceDataDiagnostic
                {
                    Code = "NoData",
                    Message = "Yahoo Finance returned no OHLC bars for this ticker and date range."
                });

            results.Add(CreateTickerResult(ticker, tickerBars, diagnostics));
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
        IReadOnlyCollection<YahooFinanceDataDiagnostic> diagnostics) =>
        new()
        {
            Ticker = ticker,
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
