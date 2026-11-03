using Njord.Data.MarketData.Contracts;

namespace Njord.Data.MarketData.Tests;

public sealed class MarketDataProviderSelectorTests
{
    #region Provider selection

    [Fact]
    public void GetProvider_SelectsRegisteredProviderWithoutCaseSensitivity()
    {
        var provider = new StubMarketDataProvider("YahooFinance");
        var selector = new MarketDataProviderSelector([provider]);

        Assert.Same(provider, selector.GetProvider("yahoofinance"));
    }

    [Fact]
    public void GetProvider_ThrowsForUnknownName()
    {
        var selector = new MarketDataProviderSelector([new StubMarketDataProvider("Massive")]);

        var exception = Assert.Throws<KeyNotFoundException>(() => selector.GetProvider("Unknown"));

        Assert.Contains("Unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsDuplicateNamesIgnoringCase()
    {
        var providers = new IMarketDataProvider[]
        {
            new StubMarketDataProvider("Massive"),
            new StubMarketDataProvider("massive")
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MarketDataProviderSelector(providers));

        Assert.Contains("More than one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsProviderWithoutStableName()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new MarketDataProviderSelector([new StubMarketDataProvider(" ")]));

        Assert.Contains("non-empty provider name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CorporateActionCapability_IsOptionalForMarketDataProviders()
    {
        IMarketDataProvider provider = new StubMarketDataProvider("OHLCOnly");

        Assert.False(provider is ICorporateActionProvider);
    }

    [Fact]
    public async Task CorporateActionCapability_ProvidesVectorCorporateActionResults()
    {
        var provider = new StubCorporateActionProvider("Actions");
        var request = new CorporateActionHistoricalRequest
        {
            Tickers = ["AAPL", "MSFT"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31)
        };

        var results = await provider.GetCorporateActionsAsync(request);

        Assert.Equal(["AAPL", "MSFT"], results.Select(result => result.Ticker));
    }

    #endregion

    #region Helpers

    private sealed class StubMarketDataProvider(string providerName) : IMarketDataProvider
    {
        public string ProviderName { get; } = providerName;

        public Task<IReadOnlyList<MarketDataTickerOhlcResult>> GetHistoricalOhlcAsync(
            MarketDataHistoricalOhlcRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketDataTickerOhlcResult>>([]);
    }

    private sealed class StubCorporateActionProvider(string providerName) : ICorporateActionProvider
    {
        public string ProviderName { get; } = providerName;

        public Task<IReadOnlyList<MarketDataTickerOhlcResult>> GetHistoricalOhlcAsync(
            MarketDataHistoricalOhlcRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketDataTickerOhlcResult>>([]);

        public Task<IReadOnlyList<MarketDataTickerCorporateActionResult>> GetCorporateActionsAsync(
            CorporateActionHistoricalRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketDataTickerCorporateActionResult>>(
            [
                ..request.Tickers.Select(ticker => new MarketDataTickerCorporateActionResult
                {
                    Ticker = ticker,
                    Actions = [],
                    Diagnostics = []
                })
            ]);
    }

    #endregion
}
