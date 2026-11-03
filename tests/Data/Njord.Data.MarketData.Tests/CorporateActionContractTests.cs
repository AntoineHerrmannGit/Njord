using Njord.Data.MarketData.Contracts;

namespace Njord.Data.MarketData.Tests;

public sealed class CorporateActionContractTests
{
    #region Request validation

    [Fact]
    public void Validate_AcceptsVectorInclusiveDateRange()
    {
        CreateRequest().Validate();
    }

    [Fact]
    public void Validate_RejectsEmptyTickerList()
    {
        var request = CreateRequest() with { Tickers = [] };

        Assert.Throws<ArgumentException>(request.Validate);
    }

    [Fact]
    public void Validate_RejectsMaximumDateThatCannotHaveExclusiveBoundary()
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
    public void Validate_RejectsReversedDateRange()
    {
        var request = CreateRequest() with
        {
            From = new DateOnly(2025, 2, 1),
            To = new DateOnly(2025, 1, 31)
        };

        Assert.Throws<ArgumentException>(request.Validate);
    }

    #endregion

    #region Ticker result immutability

    [Fact]
    public void TickerResult_CopiesActionsAndDiagnostics()
    {
        var inputActions = new List<MarketCorporateAction>
        {
            new()
            {
                Kind = CorporateActionKind.Split,
                ExecutionDate = new DateOnly(2025, 1, 15),
                SplitRatio = "2:1",
                EventId = "provider-event-1"
            }
        };
        var diagnostic = new MarketDataDiagnostic
        {
            Code = "PartialData",
            Message = "Some events were omitted.",
            AffectedCount = 1,
            FirstAffectedTimestamp = DateTimeOffset.UnixEpoch
        };
        var inputDiagnostics = new List<MarketDataDiagnostic> { diagnostic };
        var result = new MarketDataTickerCorporateActionResult
        {
            Ticker = "AAPL",
            Actions = inputActions,
            Diagnostics = inputDiagnostics
        };
        inputActions.Clear();
        inputDiagnostics.Clear();

        Assert.True(result.HasData);
        Assert.Equal("2:1", Assert.Single(result.Actions).SplitRatio);
        Assert.Same(diagnostic, Assert.Single(result.Diagnostics));
        Assert.Throws<NotSupportedException>(() => ((IList<MarketCorporateAction>)result.Actions).Clear());
    }

    [Fact]
    public void TickerResultAllowsNoProviderSuppliedActionDetails()
    {
        var action = new MarketCorporateAction { Kind = CorporateActionKind.Ipo };

        Assert.Null(action.ExecutionDate);
        Assert.Null(action.ExDividendDate);
        Assert.Null(action.EffectiveDate);
        Assert.Null(action.AnnouncementDate);
        Assert.Null(action.RecordDate);
        Assert.Null(action.PayDate);
        Assert.Null(action.SplitRatio);
        Assert.Null(action.SplitFrom);
        Assert.Null(action.SplitTo);
        Assert.Null(action.DividendAmount);
        Assert.Null(action.DividendCurrency);
        Assert.Null(action.RelatedTicker);
        Assert.Null(action.Description);
        Assert.Null(action.Status);
        Assert.Null(action.IssuerName);
        Assert.Null(action.PrimaryExchange);
        Assert.Null(action.CurrencyCode);
        Assert.Null(action.ProviderActionType);
        Assert.Null(action.EventId);
    }

    #endregion

    #region Helpers

    private static CorporateActionHistoricalRequest CreateRequest() =>
        new()
        {
            Tickers = ["AAPL", "MSFT"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31)
        };

    #endregion
}
