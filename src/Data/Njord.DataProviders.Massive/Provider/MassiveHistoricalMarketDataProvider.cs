using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Mapping;
using Njord.DataProviders.Massive.Transport;
using Njord.Models.MarketData;

namespace Njord.DataProviders.Massive.Provider;

internal sealed class MassiveHistoricalMarketDataProvider : IMassiveHistoricalMarketDataProvider
{
    #region Provider dependencies

    private readonly IMassiveAggregatesApiClient _apiClient;
    private readonly IMassiveOhlcMapper _mapper;

    public MassiveHistoricalMarketDataProvider(
        IMassiveAggregatesApiClient apiClient,
        IMassiveOhlcMapper mapper)
    {
        _apiClient = apiClient;
        _mapper = mapper;
    }

    #endregion

    #region Historical market data

    public async Task<IReadOnlyList<Ohlc>> GetHistoricalOhlcAsync(
        MassiveHistoricalOhlcRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var pages = await _apiClient
            .GetHistoricalAggregatesAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return _mapper.Map(pages, request);
    }

    #endregion
}
