using System.Globalization;
using Njord.Core.Mapping;
using Njord.Data.MarketData.Contracts;
using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Errors;
using Njord.DataProviders.Massive.Mapping;
using Njord.DataProviders.Massive.Serialization;
using Njord.DataProviders.Massive.Transport;
using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Provider;

internal sealed class MassiveHistoricalMarketDataProvider :
    IMassiveHistoricalMarketDataProvider,
    IMarketDataProvider,
    IMarketDataProvider<IMarketData>,
    ICorporateActionProvider
{
    #region Provider dependencies

    private readonly IMassiveAggregatesApiClient _apiClient;
    private readonly IMassiveCorporateActionsApiClient _corporateActionsApiClient;
    private readonly IMapper<MassiveOhlcMappingInput, IReadOnlyList<Ohlc>> _mapper;
    private readonly IInstrumentReferential _referential;

    public MassiveHistoricalMarketDataProvider(
        IMassiveAggregatesApiClient apiClient,
        IMassiveCorporateActionsApiClient corporateActionsApiClient,
        IMapper<MassiveOhlcMappingInput, IReadOnlyList<Ohlc>> mapper,
        IInstrumentReferential referential)
    {
        _apiClient = apiClient;
        _corporateActionsApiClient = corporateActionsApiClient;
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
            bars.AddRange(_mapper.Map(new MassiveOhlcMappingInput
            {
                Pages = pages,
                Request = resolvedRequest
            }));
        }

        return bars
            .OrderBy(bar => tickerOrder[bar.Ticker.ToUpperInvariant()])
            .ThenBy(bar => bar.Timestamp)
            .ToArray();
    }

    #endregion

    #region Common market-data contract

    public string ProviderName => "Massive";

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
                            "Massive returned an OHLC result incompatible with the requested market-data type."))
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
                            "Massive returned a corporate-action result incompatible with the requested market-data type."))
                    .ToArray());
        }

        throw new NotSupportedException(
            $"Massive does not support request '{request.GetType().Name}' for market-data type '{typeof(TMarketData).Name}'.");
    }

    public async Task<IReadOnlyList<MarketDataTickerOhlcResult>> GetHistoricalOhlcAsync(
        MarketDataHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var results = new List<MarketDataTickerOhlcResult>(request.Tickers.Count);
        foreach (var ticker in request.Tickers)
        {
            IReadOnlyList<Ohlc> tickerBars;
            try
            {
                tickerBars = await GetHistoricalOhlcAsync(
                    new MassiveHistoricalOhlcRequest
                    {
                        Tickers = [ticker],
                        From = request.From,
                        To = request.To,
                        Multiplier = request.Multiplier,
                        Timespan = MapInterval(request.Interval),
                        Adjusted = false
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (TickerReferenceResolutionException exception)
            {
                results.Add(new MarketDataTickerOhlcResult
                {
                    Ticker = ticker,
                    Bars = [],
                    Diagnostics =
                    [
                        new MarketDataDiagnostic
                        {
                            Code = "TickerMappingError",
                            Message = exception.Message
                        }
                    ]
                });
                continue;
            }

            var rawTickerBars = tickerBars
                .Select(bar => bar.IsAdjusted.HasValue ? bar : bar with { IsAdjusted = false })
                .ToArray();
            var diagnostics = rawTickerBars.Length > 0
                ? Array.Empty<MarketDataDiagnostic>()
                : new[]
                {
                    new MarketDataDiagnostic
                    {
                        Code = "NoData",
                        Message = $"Massive returned no OHLC bars for ticker '{ticker}' and the requested date range."
                    }
                };
            results.Add(new MarketDataTickerOhlcResult
            {
                Ticker = ticker,
                Bars = rawTickerBars,
                Diagnostics = diagnostics
            });
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static MassiveTimespan MapInterval(MarketDataInterval interval) =>
        interval switch
        {
            MarketDataInterval.Second => MassiveTimespan.Second,
            MarketDataInterval.Minute => MassiveTimespan.Minute,
            MarketDataInterval.Hour => MassiveTimespan.Hour,
            MarketDataInterval.Day => MassiveTimespan.Day,
            MarketDataInterval.Session => MassiveTimespan.Session,
            MarketDataInterval.Week => MassiveTimespan.Week,
            MarketDataInterval.Month => MassiveTimespan.Month,
            MarketDataInterval.Quarter => MassiveTimespan.Quarter,
            MarketDataInterval.Year => MassiveTimespan.Year,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval is not supported.")
        };

    #endregion

    #region Corporate actions

    public async Task<IReadOnlyList<MarketDataTickerCorporateActionResult>> GetCorporateActionsAsync(
        CorporateActionHistoricalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

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
                    Message = "Massive endpoints provide splits, stock dividends, cash dividends, ticker changes, and IPOs; mergers and delistings are not available through these endpoints."
                }
            };
            IReadOnlyList<TickerProviderMapping> mappings;
            try
            {
                mappings = await _referential
                    .ResolveAsync(ticker, "Massive", request.From, request.To, cancellationToken)
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
                    Message = $"No Massive ticker mapping covers '{ticker}' and the requested date range."
                });
                results.Add(CreateCorporateActionResult(ticker, actions, diagnostics));
                continue;
            }

            foreach (var mapping in mappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (mapping.AssetClass != NjordAssetClass.Stock)
                {
                    diagnostics.Add(new MarketDataDiagnostic
                    {
                        Code = "UnsupportedAssetClass",
                        Message = $"Massive corporate-action endpoints are available only for stocks; '{ticker}' is mapped as '{mapping.AssetClass}'."
                    });
                    continue;
                }

                var mappingFrom = DateOnly.FromDateTime(mapping.StartDate);
                var mappingTo = DateOnly.FromDateTime(mapping.EndDate);
                var from = mappingFrom > request.From ? mappingFrom : request.From;
                var to = mappingTo < request.To ? mappingTo : request.To;

                var splits = await _corporateActionsApiClient
                    .GetHistoricalStockSplitsAsync(mapping.ProviderTicker, from, to, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var split in splits)
                    MapSplit(split, mapping.ProviderTicker, actions, diagnostics);

                var dividends = await _corporateActionsApiClient
                    .GetHistoricalStockDividendsAsync(mapping.ProviderTicker, from, to, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var dividend in dividends)
                    MapDividend(dividend, mapping.ProviderTicker, actions, diagnostics);

                var tickerEvents = await _corporateActionsApiClient
                    .GetTickerEventsAsync(mapping.ProviderTicker, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var tickerEvent in tickerEvents)
                    MapTickerEvent(tickerEvent, from, to, actions, diagnostics);

                var ipos = await _corporateActionsApiClient
                    .GetHistoricalStockIposAsync(mapping.ProviderTicker, from, to, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var ipo in ipos)
                    MapIpo(ipo, mapping.ProviderTicker, actions, diagnostics);
            }

            if (actions.Count == 0)
                diagnostics.Add(new MarketDataDiagnostic
                {
                    Code = "NoCorporateActions",
                    Message = $"Massive returned no supported corporate actions for ticker '{ticker}' and the requested date range."
                });

            results.Add(CreateCorporateActionResult(ticker, actions, diagnostics));
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static void MapSplit(
        MassiveStockSplitDto split,
        string providerTicker,
        ICollection<MarketCorporateAction> actions,
        ICollection<MarketDataDiagnostic> diagnostics)
    {
        if (!IsRequestedProviderTicker(split.Ticker, providerTicker))
            throw new MassiveDataProviderException("Massive returned a split for a different ticker.");
        if (!TryParseProviderDate(split.ExecutionDate, out var executionDate) ||
            !split.SplitFrom.HasValue ||
            !split.SplitTo.HasValue ||
            split.SplitFrom <= 0 ||
            split.SplitTo <= 0)
        {
            AddInvalidActionDiagnostic(diagnostics, "InvalidSplitEvent", "Massive returned a split with an invalid date or ratio.");
            return;
        }

        actions.Add(new MarketCorporateAction
        {
            Kind = string.Equals(split.AdjustmentType, "stock_dividend", StringComparison.OrdinalIgnoreCase)
                ? CorporateActionKind.StockDividend
                : CorporateActionKind.Split,
            ExecutionDate = executionDate,
            SplitFrom = split.SplitFrom,
            SplitTo = split.SplitTo,
            ProviderActionType = split.AdjustmentType,
            EventId = split.Id
        });
    }

    private static void MapDividend(
        MassiveStockDividendDto dividend,
        string providerTicker,
        ICollection<MarketCorporateAction> actions,
        ICollection<MarketDataDiagnostic> diagnostics)
    {
        if (!IsRequestedProviderTicker(dividend.Ticker, providerTicker))
            throw new MassiveDataProviderException("Massive returned a dividend for a different ticker.");
        if (!TryParseProviderDate(dividend.ExDividendDate, out var exDividendDate) ||
            !dividend.CashAmount.HasValue)
        {
            AddInvalidActionDiagnostic(
                diagnostics,
                "InvalidDividendEvent",
                "Massive returned a dividend without a valid ex-dividend date or cash amount.");
            return;
        }

        actions.Add(new MarketCorporateAction
        {
            Kind = CorporateActionKind.Dividend,
            ExDividendDate = exDividendDate,
            DividendAmount = dividend.CashAmount,
            DividendCurrency = dividend.Currency,
            AnnouncementDate = ParseOptionalProviderDate(
                dividend.DeclarationDate,
                "declaration date",
                diagnostics),
            RecordDate = ParseOptionalProviderDate(dividend.RecordDate, "record date", diagnostics),
            PayDate = ParseOptionalProviderDate(dividend.PayDate, "pay date", diagnostics),
            ProviderActionType = dividend.DistributionType,
            EventId = dividend.Id
        });
    }

    private static void MapTickerEvent(
        MassiveTickerEventDto tickerEvent,
        DateOnly from,
        DateOnly to,
        ICollection<MarketCorporateAction> actions,
        ICollection<MarketDataDiagnostic> diagnostics)
    {
        if (!string.Equals(tickerEvent.Type, "ticker_change", StringComparison.OrdinalIgnoreCase))
            return;
        if (!TryParseProviderDate(tickerEvent.Date, out var effectiveDate) ||
            string.IsNullOrWhiteSpace(tickerEvent.TickerChange?.Ticker))
        {
            AddInvalidActionDiagnostic(
                diagnostics,
                "InvalidTickerChangeEvent",
                "Massive returned a ticker-change event without a valid date or related ticker.");
            return;
        }

        if (effectiveDate < from || effectiveDate > to)
            return;

        actions.Add(new MarketCorporateAction
        {
            Kind = CorporateActionKind.TickerChange,
            EffectiveDate = effectiveDate,
            RelatedTicker = tickerEvent.TickerChange.Ticker,
            ProviderActionType = tickerEvent.Type
        });
    }

    private static void MapIpo(
        MassiveStockIpoDto ipo,
        string providerTicker,
        ICollection<MarketCorporateAction> actions,
        ICollection<MarketDataDiagnostic> diagnostics)
    {
        if (!IsRequestedProviderTicker(ipo.Ticker, providerTicker))
            throw new MassiveDataProviderException("Massive returned an IPO event for a different ticker.");
        if (!TryParseProviderDate(ipo.ListingDate, out var listingDate))
        {
            AddInvalidActionDiagnostic(
                diagnostics,
                "InvalidIpoEvent",
                "Massive returned an IPO without a valid listing date.");
            return;
        }

        actions.Add(new MarketCorporateAction
        {
            Kind = CorporateActionKind.Ipo,
            EffectiveDate = listingDate,
            AnnouncementDate = ParseOptionalProviderDate(ipo.AnnouncedDate, "announcement date", diagnostics),
            Status = ipo.IpoStatus,
            IssuerName = ipo.IssuerName,
            PrimaryExchange = ipo.PrimaryExchange,
            CurrencyCode = ipo.CurrencyCode
        });
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

    private static bool IsRequestedProviderTicker(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.Ordinal);

    private static bool TryParseProviderDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static DateOnly? ParseOptionalProviderDate(
        string? value,
        string fieldName,
        ICollection<MarketDataDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (TryParseProviderDate(value, out var date))
            return date;

        AddInvalidActionDiagnostic(
            diagnostics,
            "InvalidOptionalEventDate",
            $"Massive returned an invalid {fieldName}; the corporate action was preserved without that optional date.");
        return null;
    }

    private static void AddInvalidActionDiagnostic(
        ICollection<MarketDataDiagnostic> diagnostics,
        string code,
        string message) =>
        diagnostics.Add(new MarketDataDiagnostic { Code = code, Message = message });

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
        if (assetClass == MassiveAssetClass.Future && request.Adjusted == true)
            throw new ArgumentException(
                $"Massive futures aggregates do not support split adjustment for ticker '{njordTicker}'.",
                nameof(request));

        return new MassiveResolvedOhlcRequest
        {
            NjordTicker = mapping.Ticker,
            Ticker = mapping.ProviderTicker,
            AssetClass = assetClass,
            From = from,
            To = to,
            Multiplier = request.Multiplier,
            Timespan = timespan,
            Adjusted = assetClass == MassiveAssetClass.Future ? null : request.Adjusted
        };
    }

    #endregion
}
