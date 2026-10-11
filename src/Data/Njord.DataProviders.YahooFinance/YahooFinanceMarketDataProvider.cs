using Njord.MarketData;
using Njord.Core.Serialization;
using Njord.DataProviders.YahooFinance.Chart;
using Njord.DataProviders.YahooFinance.Options;
using Njord.DataProviders.YahooFinance.Transport;

namespace Njord.DataProviders.YahooFinance;

/// <summary>Provides provider-neutral market data sourced from Yahoo Finance.</summary>
public sealed class YahooFinanceMarketDataProvider : IMarketDataProvider
{
    private readonly YahooFinanceChartDataSource _chartDataSource;
    private readonly YahooFinanceOptionChainDataSource _optionChainDataSource;

    /// <summary>Creates the provider using an application-managed HTTP client.</summary>
    /// <param name="httpClient">HTTP client configured by the application.</param>
    public YahooFinanceMarketDataProvider(HttpClient httpClient)
        : this(httpClient, new SystemTextJsonSerializer())
    {
    }

    /// <summary>Creates the provider with an application-managed HTTP client and shared JSON serializer.</summary>
    /// <param name="httpClient">HTTP client configured by the application.</param>
    /// <param name="jsonSerializer">Solution-wide JSON serialization service.</param>
    public YahooFinanceMarketDataProvider(HttpClient httpClient, IJsonSerializer jsonSerializer)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(jsonSerializer);

        var transport = new YahooFinanceHttpClient(httpClient, jsonSerializer);
        _chartDataSource = new YahooFinanceChartDataSource(transport);
        _optionChainDataSource = new YahooFinanceOptionChainDataSource(transport);
    }

    /// <inheritdoc />
    public string ProviderId => "YahooFinance";

    /// <inheritdoc />
    public Task<IReadOnlyList<MarketDataBar>> GetHistoricalBarsAsync(
        HistoricalMarketDataRequest request,
        CancellationToken cancellationToken = default) =>
        _chartDataSource.GetHistoricalBarsAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<MarketDataQuote> GetQuoteAsync(
        MarketInstrument instrument,
        CancellationToken cancellationToken = default) =>
        _chartDataSource.GetQuoteAsync(instrument, cancellationToken);

    /// <inheritdoc />
    public Task<MarketOptionChain> GetOptionChainAsync(
        OptionChainRequest request,
        CancellationToken cancellationToken = default) =>
        _optionChainDataSource.GetOptionChainAsync(request, cancellationToken);
}
