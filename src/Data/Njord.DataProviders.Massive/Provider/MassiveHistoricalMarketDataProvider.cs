using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;
using Njord.DataProviders.Massive.Mapping;
using Njord.DataProviders.Massive.Transport;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Models;
using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Provider;

internal sealed class MassiveHistoricalMarketDataProvider : IMassiveHistoricalMarketDataProvider
{
    #region Provider dependencies

    private readonly IMassiveAggregatesApiClient _apiClient;
    private readonly IMassiveOhlcMapper _mapper;
    private readonly IInstrumentReferential _referential;

    public MassiveHistoricalMarketDataProvider(
        IMassiveAggregatesApiClient apiClient,
        IMassiveOhlcMapper mapper,
        IInstrumentReferential referential)
    {
        _apiClient = apiClient;
        _mapper = mapper;
        _referential = referential;
    }

    #endregion

    #region Historical market data

    public async Task<IReadOnlyList<Ohlc>> GetHistoricalOhlcAsync(
        MassiveHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var resolvedRequests = new List<MassiveResolvedOhlcRequest>();
        foreach (var ticker in request.Tickers)
        {
            var mappings = await _referential
                .ResolveAsync(ticker, "Massive", request.From, request.To, cancellationToken)
                .ConfigureAwait(false);

            foreach (var mapping in mappings)
            {
                var resolvedRequest = CreateResolvedRequest(ticker, mapping, request);
                resolvedRequest.Validate();
                resolvedRequests.Add(resolvedRequest);
            }
        }

        var tickerOrder = request.Tickers
            .Select((ticker, index) => new { ticker, index })
            .ToDictionary(
                item => item.ticker.Trim().ToUpperInvariant(),
                item => item.index,
                StringComparer.Ordinal);
        var bars = new List<Ohlc>();
        foreach (var resolvedRequest in resolvedRequests)
        {
            var pages = await _apiClient
                .GetHistoricalAggregatesAsync(resolvedRequest, cancellationToken)
                .ConfigureAwait(false);
            bars.AddRange(_mapper.Map(pages, resolvedRequest));
        }

        return bars
            .OrderBy(bar => tickerOrder[bar.Ticker.ToUpperInvariant()])
            .ThenBy(bar => bar.Timestamp)
            .ToArray();
    }

    #endregion

    #region Referential mapping

    private static MassiveResolvedOhlcRequest CreateResolvedRequest(
        string njordTicker,
        TickerProviderMapping mapping,
        MassiveHistoricalOhlcRequest request)
    {
        var assetClass = mapping.AssetClass switch
        {
            NjordAssetClass.Stock => MassiveAssetClass.Stock,
            NjordAssetClass.Index => MassiveAssetClass.Index,
            NjordAssetClass.Option => MassiveAssetClass.Option,
            NjordAssetClass.Future => MassiveAssetClass.Future,
            NjordAssetClass.Crypto => MassiveAssetClass.Crypto,
            NjordAssetClass.Forex => MassiveAssetClass.Forex,
            _ => throw new MassiveDataProviderException(
                $"Massive does not support Njord asset class '{mapping.AssetClass}' for ticker '{njordTicker}'.")
        };
        var mappingStart = DateOnly.FromDateTime(mapping.StartDate);
        var mappingEnd = DateOnly.FromDateTime(mapping.EndDate);
        var from = mappingStart > request.From ? mappingStart : request.From;
        var to = mappingEnd < request.To ? mappingEnd : request.To;
        var timespan = assetClass == MassiveAssetClass.Future && request.Timespan == MassiveTimespan.Day
            ? MassiveTimespan.Session
            : request.Timespan;

        return new MassiveResolvedOhlcRequest
        {
            NjordTicker = mapping.Ticker,
            Ticker = mapping.ProviderTicker,
            AssetClass = assetClass,
            From = from,
            To = to,
            Multiplier = request.Multiplier,
            Timespan = timespan,
            Adjusted = request.Adjusted
        };
    }

    #endregion
}
