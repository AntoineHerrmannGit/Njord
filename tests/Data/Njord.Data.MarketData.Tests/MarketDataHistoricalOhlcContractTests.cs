using Njord.Data.MarketData.Contracts;
using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Tests;

public sealed class MarketDataHistoricalOhlcContractTests
{
    #region Request validation

    [Fact]
    public void MarketDataInterval_ContainsEveryMassiveUnit()
    {
        Assert.Equal(
            [
                MarketDataInterval.Second,
                MarketDataInterval.Minute,
                MarketDataInterval.Hour,
                MarketDataInterval.Day,
                MarketDataInterval.Session,
                MarketDataInterval.Week,
                MarketDataInterval.Month,
                MarketDataInterval.Quarter,
                MarketDataInterval.Year
            ],
            Enum.GetValues<MarketDataInterval>());
    }

    [Fact]
    public void Validate_AcceptsVectorInclusiveDateRange()
    {
        var request = CreateRequest();

        request.Validate();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsNonPositiveMultiplier(int multiplier)
    {
        var request = CreateRequest() with { Multiplier = multiplier };

        Assert.Throws<ArgumentOutOfRangeException>(request.Validate);
    }

    [Fact]
    public void Validate_RejectsDateRangeInReverse()
    {
        var request = CreateRequest() with
        {
            From = new DateOnly(2025, 2, 1),
            To = new DateOnly(2025, 1, 1)
        };

        Assert.Throws<ArgumentException>(request.Validate);
    }

    [Fact]
    public void Validate_RejectsMaximumEndDateThatCannotHaveExclusiveBoundary()
    {
        var request = CreateRequest() with { To = DateOnly.MaxValue };

        Assert.Throws<ArgumentOutOfRangeException>(request.Validate);
    }

    [Fact]
    public void Validate_RejectsDuplicateTickersIgnoringCaseAndOuterWhitespace()
    {
        var request = CreateRequest() with { Tickers = ["AAPL", " aapl "] };

        Assert.Throws<ArgumentException>(request.Validate);
    }

    [Fact]
    public void Validate_RejectsEmptyTickerList()
    {
        var request = CreateRequest() with { Tickers = [] };

        Assert.Throws<ArgumentException>(request.Validate);
    }

    [Fact]
    public void Validate_RejectsUndefinedInterval()
    {
        var request = CreateRequest() with { Interval = (MarketDataInterval)999 };

        Assert.Throws<ArgumentOutOfRangeException>(request.Validate);
    }

    #endregion

    #region Ticker result immutability

    [Fact]
    public void TickerResult_CopiesBarsAndDiagnosticsAndAllowsMissingExchangeTimeZone()
    {
        var inputBars = new List<Ohlc> { CreateBar("AAPL") };
        var diagnostic = new MarketDataDiagnostic
        {
            Code = "PartialData",
            Message = "Some observations were omitted.",
            AffectedCount = 1,
            FirstAffectedTimestamp = DateTimeOffset.UnixEpoch
        };
        var inputDiagnostics = new List<MarketDataDiagnostic> { diagnostic };
        var result = new MarketDataTickerOhlcResult
        {
            Ticker = "AAPL",
            Bars = inputBars,
            Diagnostics = inputDiagnostics
        };
        inputBars.Clear();
        inputDiagnostics.Clear();

        Assert.Null(result.ExchangeTimeZoneId);
        Assert.Single(result.Bars);
        Assert.Same(result.Bars, result.Data);
        Assert.Same(diagnostic, Assert.Single(result.Diagnostics));
        Assert.Throws<NotSupportedException>(() => ((IList<Ohlc>)result.Bars).Clear());
    }

    [Fact]
    public void GenericContracts_AssociateRequestsAndResultsWithTheirMarketDataTypes()
    {
        IMarketDataRequest<Ohlc> ohlcRequest = CreateRequest();
        IMarketDataRequest<MarketCorporateAction> actionRequest = new CorporateActionHistoricalRequest
        {
            Tickers = ["AAPL"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31)
        };
        IMarketDataResult<Ohlc> ohlcResult = new MarketDataTickerOhlcResult
        {
            Ticker = "AAPL",
            Bars = [CreateBar("AAPL")],
            Diagnostics = []
        };
        IMarketDataResult<MarketCorporateAction> actionResult = new MarketDataTickerCorporateActionResult
        {
            Ticker = "AAPL",
            Actions = [],
            Diagnostics = []
        };

        Assert.Equal("AAPL", ohlcResult.Ticker);
        Assert.Single(ohlcResult.Data);
        Assert.Equal("AAPL", actionResult.Ticker);
        Assert.Empty(actionResult.Data);
        Assert.IsAssignableFrom<IMarketData>(Assert.Single(ohlcResult.Data));
        Assert.IsAssignableFrom<IMarketDataRequest<MarketCorporateAction>>(actionRequest);
        Assert.IsAssignableFrom<IMarketDataRequest<Ohlc>>(ohlcRequest);
    }

    [Fact]
    public void CorporateActionResult_DataIsSnapshotAliasOfActions()
    {
        var actions = new List<MarketCorporateAction>
        {
            new() { Kind = CorporateActionKind.Split }
        };
        var result = new MarketDataTickerCorporateActionResult
        {
            Ticker = "AAPL",
            Actions = actions,
            Diagnostics = []
        };
        actions.Clear();

        Assert.Same(result.Actions, result.Data);
        Assert.Single(result.Data);
        Assert.Throws<NotSupportedException>(() => ((IList<MarketCorporateAction>)result.Data).Clear());
    }

    [Fact]
    public void TickerResult_ExchangeTimeZoneIdCanBeProvided()
    {
        var result = new MarketDataTickerOhlcResult
        {
            Ticker = "AAPL",
            Bars = [],
            Diagnostics = [],
            ExchangeTimeZoneId = "America/New_York"
        };

        Assert.Equal("America/New_York", result.ExchangeTimeZoneId);
    }

    #endregion

    #region Helpers

    private static MarketDataHistoricalOhlcRequest CreateRequest() =>
        new()
        {
            Tickers = ["AAPL", "MSFT"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Interval = MarketDataInterval.Day
        };

    private static Ohlc CreateBar(string ticker) =>
        new()
        {
            Ticker = ticker,
            Open = 10,
            High = 12,
            Low = 9,
            Close = 11,
            Timestamp = DateTimeOffset.UnixEpoch
        };

    #endregion
}
