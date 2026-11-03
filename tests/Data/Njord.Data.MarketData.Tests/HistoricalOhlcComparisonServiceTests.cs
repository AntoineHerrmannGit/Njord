using Njord.Data.MarketData.Comparison;
using Njord.Data.MarketData.Contracts;
using Njord.Models.MarketData;

namespace Njord.Data.MarketData.Tests;

public sealed class HistoricalOhlcComparisonServiceTests
{
    #region Comparison formula and bar matching

    [Fact]
    public void Compare_UsesFixedOnePercentThresholdAndReportsPerFieldPercentages()
    {
        var left = CreateResult("AAPL", [CreateBar("AAPL", 0, 100, 100, 100, 100)]);
        var right = CreateResult("AAPL", [CreateBar("AAPL", 0, 101, 102, 0, 100)]);

        var result = Compare(left, right);

        Assert.Equal(MarketDataComparisonStatus.Fail, result.Status);
        Assert.Equal(1, result.MatchedBarCount);
        Assert.Equal(1, result.ComparedBarCount);
        var fields = Assert.Single(result.MatchedBars).Fields;
        Assert.Equal(1m, fields.Single(field => field.Field == MarketDataOhlcField.Open).PercentageDifference);
        Assert.Equal(MarketDataComparisonStatus.Pass, fields.Single(field => field.Field == MarketDataOhlcField.Open).Status);
        Assert.Equal(2m, fields.Single(field => field.Field == MarketDataOhlcField.High).PercentageDifference);
        Assert.Equal(MarketDataComparisonStatus.Fail, fields.Single(field => field.Field == MarketDataOhlcField.High).Status);
        Assert.Null(fields.Single(field => field.Field == MarketDataOhlcField.Low).PercentageDifference);
        Assert.Equal(MarketDataComparisonStatus.Fail, fields.Single(field => field.Field == MarketDataOhlcField.Low).Status);
        Assert.Equal(0m, fields.Single(field => field.Field == MarketDataOhlcField.Close).PercentageDifference);
    }

    [Fact]
    public void Compare_TreatsTwoZeroValuesAsEqual()
    {
        var result = Compare(
            CreateResult("AAPL", [CreateBar("AAPL", 0, 0, 0, 0, 0)]),
            CreateResult("AAPL", [CreateBar("AAPL", 0, 0, 0, 0, 0)]));

        Assert.Equal(MarketDataComparisonStatus.Pass, result.Status);
        Assert.All(
            Assert.Single(result.MatchedBars).Fields,
            field =>
            {
                Assert.Equal(0m, field.PercentageDifference);
                Assert.Equal(MarketDataComparisonStatus.Pass, field.Status);
            });
    }

    [Fact]
    public void Compare_MatchesDailyBarsByEachExchangeLocalDate()
    {
        var leftBar = CreateBar(
            "AAPL",
            DateTimeOffset.Parse("2025-01-02T00:30:00Z"),
            100,
            101,
            99,
            100);
        var rightBar = CreateBar(
            "AAPL",
            DateTimeOffset.Parse("2025-01-01T15:00:00Z"),
            100,
            101,
            99,
            100);
        var result = Compare(
            CreateResult("AAPL", [leftBar], "America/Los_Angeles"),
            CreateResult("AAPL", [rightBar], "America/New_York"),
            MarketDataInterval.Day);

        Assert.Equal(MarketDataComparisonStatus.Pass, result.Status);
        var barComparison = Assert.Single(result.MatchedBars);
        Assert.Equal(new DateOnly(2025, 1, 1), barComparison.ExchangeLocalDate);
        Assert.NotEqual(barComparison.LeftBarStart, barComparison.RightBarStart);
    }

    [Fact]
    public void Compare_MatchesIntradayBarsOnlyByExactStartInstant()
    {
        var left = CreateResult(
            "AAPL",
            [CreateBar("AAPL", DateTimeOffset.Parse("2025-01-01T15:00:00Z"), 100, 101, 99, 100)]);
        var right = CreateResult(
            "AAPL",
            [CreateBar("AAPL", DateTimeOffset.Parse("2025-01-01T15:01:00Z"), 100, 101, 99, 100)]);

        var result = Compare(left, right, MarketDataInterval.Minute);

        Assert.Equal(MarketDataComparisonStatus.Inconclusive, result.Status);
        Assert.Equal(0, result.MatchedBarCount);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UnmatchedBars");
    }

    #endregion

    #region Required comparison metadata and diagnostics

    [Fact]
    public void Compare_RequiresBothBarsToExplicitlyReportUnadjustedPrices()
    {
        var left = CreateResult("AAPL", [CreateBar("AAPL", 0, 100, 100, 100, 100)]);
        var right = CreateResult(
            "AAPL",
            [CreateBar("AAPL", 0, 100, 100, 100, 100, isAdjusted: null)]);

        var result = Compare(left, right);

        Assert.Equal(MarketDataComparisonStatus.Inconclusive, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UnknownPriceBasis");
        Assert.All(
            Assert.Single(result.MatchedBars).Fields,
            field => Assert.Equal(MarketDataComparisonStatus.Inconclusive, field.Status));
    }

    [Theory]
    [InlineData(null, "USD", "UnknownCurrency")]
    [InlineData("USD", "EUR", "CurrencyMismatch")]
    public void Compare_RequiresKnownEqualCurrencies(
        string? leftCurrency,
        string? rightCurrency,
        string diagnosticCode)
    {
        var left = CreateResult(
            "AAPL",
            [CreateBar("AAPL", 0, 100, 100, 100, 100, currency: leftCurrency)]);
        var right = CreateResult(
            "AAPL",
            [CreateBar("AAPL", 0, 100, 100, 100, 100, currency: rightCurrency)]);

        var result = Compare(left, right);

        Assert.Equal(MarketDataComparisonStatus.Inconclusive, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == diagnosticCode);
    }

    [Theory]
    [InlineData(null, "UTC", "MissingExchangeTimeZone")]
    [InlineData("not/a-zone", "UTC", "InvalidExchangeTimeZone")]
    public void Compare_RequiresValidExchangeTimezoneMetadata(
        string? leftTimeZone,
        string? rightTimeZone,
        string diagnosticCode)
    {
        var left = CreateResult("AAPL", [CreateBar("AAPL", 0, 100, 100, 100, 100)], leftTimeZone);
        var right = CreateResult("AAPL", [CreateBar("AAPL", 0, 100, 100, 100, 100)], rightTimeZone);

        var result = Compare(left, right);

        Assert.Equal(MarketDataComparisonStatus.Inconclusive, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == diagnosticCode);
    }

    [Fact]
    public void Compare_DuplicateMatchKeysMakeResultInconclusive()
    {
        Ohlc[] duplicateBars =
        [
            CreateBar("AAPL", 0, 100, 100, 100, 100),
            CreateBar("AAPL", 0, 100, 100, 100, 100)
        ];
        var result = Compare(
            CreateResult("AAPL", duplicateBars),
            CreateResult("AAPL", [CreateBar("AAPL", 0, 100, 100, 100, 100)]));

        Assert.Equal(MarketDataComparisonStatus.Inconclusive, result.Status);
        Assert.Equal(0, result.MatchedBarCount);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "DuplicateMatchKey");
    }

    [Fact]
    public void Compare_ProviderDiagnosticsMakeResultInconclusive()
    {
        var providerDiagnostic = new MarketDataDiagnostic
        {
            Code = "PartialData",
            Message = "The provider omitted a bar."
        };
        var result = Compare(
            CreateResult(
                "AAPL",
                [CreateBar("AAPL", 0, 100, 100, 100, 100)],
                diagnostics: [providerDiagnostic]),
            CreateResult("AAPL", [CreateBar("AAPL", 0, 100, 100, 100, 100)]));

        Assert.Equal(MarketDataComparisonStatus.Inconclusive, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "ProviderDiagnostic");
    }

    [Fact]
    public void Compare_FailingMatchedFieldTakesPrecedenceOverInconclusiveInputs()
    {
        var left = CreateResult(
            "AAPL",
            [
                CreateBar("AAPL", 0, 100, 100, 100, 100),
                CreateBar("AAPL", 1, 100, 100, 100, 100)
            ],
            diagnostics:
            [
                new MarketDataDiagnostic { Code = "PartialData", Message = "Provider data is incomplete." }
            ]);
        var right = CreateResult(
            "AAPL",
            [
                CreateBar("AAPL", 0, 102, 100, 100, 100),
                CreateBar("AAPL", 2, 100, 100, 100, 100)
            ]);

        var result = Compare(left, right, MarketDataInterval.Minute);

        Assert.Equal(MarketDataComparisonStatus.Fail, result.Status);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "ProviderDiagnostic");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UnmatchedBars");
    }

    #endregion

    #region Helpers

    private static MarketDataTickerOhlcComparisonResult Compare(
        MarketDataTickerOhlcResult left,
        MarketDataTickerOhlcResult right,
        MarketDataInterval interval = MarketDataInterval.Day)
    {
        var request = new MarketDataHistoricalOhlcRequest
        {
            Tickers = ["AAPL"],
            From = new DateOnly(2025, 1, 1),
            To = new DateOnly(2025, 1, 31),
            Multiplier = 1,
            Interval = interval
        };
        return Assert.Single(HistoricalOhlcComparisonService.Compare(request, [left], [right]));
    }

    private static MarketDataTickerOhlcResult CreateResult(
        string ticker,
        IReadOnlyList<Ohlc> bars,
        string? exchangeTimeZoneId = "UTC",
        IReadOnlyList<MarketDataDiagnostic>? diagnostics = null) =>
        new()
        {
            Ticker = ticker,
            Bars = bars,
            Diagnostics = diagnostics ?? [],
            ExchangeTimeZoneId = exchangeTimeZoneId
        };

    private static Ohlc CreateBar(
        string ticker,
        int minuteOffset,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        string? currency = "USD",
        bool? isAdjusted = false) =>
        CreateBar(
            ticker,
            DateTimeOffset.Parse("2025-01-01T15:00:00Z").AddMinutes(minuteOffset),
            open,
            high,
            low,
            close,
            currency,
            isAdjusted);

    private static Ohlc CreateBar(
        string ticker,
        DateTimeOffset timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        string? currency = "USD",
        bool? isAdjusted = false) =>
        new()
        {
            Ticker = ticker,
            Timestamp = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Currency = currency,
            IsAdjusted = isAdjusted
        };

    #endregion
}
