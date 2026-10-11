using Njord.Models.MarketData;

namespace Njord.Models.Tests;

public sealed class DerivedMarketDataTests
{
    #region Correlation

    [Fact]
    public void Correlation_PreservesSeriesMaturityAndKinds()
    {
        var maturity = new DateOnly(2025, 12, 19);
        var timestamp = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var correlation = new Correlation
        {
            FirstTicker = "AAPL",
            SecondTicker = "MSFT",
            Value = -0.75,
            Kind = CorrelationKind.Implied,
            Method = CorrelationMethod.Spearman,
            MaturityDate = maturity,
            Timestamp = timestamp
        };

        Assert.Equal("AAPL", correlation.FirstTicker);
        Assert.Equal("MSFT", correlation.SecondTicker);
        Assert.Equal(-0.75, correlation.Value);
        Assert.Equal(CorrelationKind.Implied, correlation.Kind);
        Assert.Equal(CorrelationMethod.Spearman, correlation.Method);
        Assert.Equal(maturity, correlation.MaturityDate);
        Assert.Equal(timestamp, correlation.Timestamp);
    }

    [Theory]
    [InlineData(-1.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Correlation_RejectsValuesOutsideCoefficientDomain(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Correlation
        {
            FirstTicker = "AAPL",
            SecondTicker = "MSFT",
            Value = value,
            Kind = CorrelationKind.Historical,
            Method = CorrelationMethod.Pearson,
            MaturityDate = DateOnly.FromDateTime(DateTime.UnixEpoch),
            Timestamp = DateTimeOffset.UnixEpoch
        });
    }

    #endregion

    #region Volatility

    [Fact]
    public void Volatility_PreservesInterpretationAndMaturity()
    {
        var maturity = new DateOnly(2025, 12, 19);
        var volatility = new Volatility
        {
            Ticker = "AAPL",
            Value = 0.2,
            Kind = VolatilityKind.Historical,
            MaturityDate = maturity,
            IsAnnualized = true,
            Timestamp = DateTimeOffset.UnixEpoch
        };

        Assert.Equal("AAPL", volatility.Ticker);
        Assert.Equal(0.2, volatility.Value);
        Assert.Equal(VolatilityKind.Historical, volatility.Kind);
        Assert.Equal(maturity, volatility.MaturityDate);
        Assert.True(volatility.IsAnnualized);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Volatility_RejectsNegativeOrNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Volatility
        {
            Ticker = "AAPL",
            Value = value,
            Kind = VolatilityKind.Historical,
            MaturityDate = DateOnly.FromDateTime(DateTime.UnixEpoch),
            IsAnnualized = true,
            Timestamp = DateTimeOffset.UnixEpoch
        });
    }

    #endregion

    #region Forward rate and discount factor

    [Fact]
    public void ForwardRate_PreservesCurvePeriodAndAllowsNegativeRates()
    {
        var forwardRate = new ForwardRate
        {
            CurveName = "USD-SOFR",
            Currency = "USD",
            Value = -0.005,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 7, 1),
            Timestamp = DateTimeOffset.UnixEpoch
        };

        Assert.Equal("USD-SOFR", forwardRate.CurveName);
        Assert.Equal("USD", forwardRate.Currency);
        Assert.Equal(-0.005, forwardRate.Value);
        Assert.Equal(new DateOnly(2026, 1, 1), forwardRate.StartDate);
        Assert.Equal(new DateOnly(2026, 7, 1), forwardRate.EndDate);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ForwardRate_RejectsNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ForwardRate
        {
            CurveName = "USD-SOFR",
            Currency = "USD",
            Value = value,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 7, 1),
            Timestamp = DateTimeOffset.UnixEpoch
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void DiscountFactor_RejectsNonPositiveOrNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscountFactor
        {
            CurveName = "USD-SOFR",
            Currency = "USD",
            Value = value,
            MaturityDate = new DateOnly(2026, 1, 1),
            Timestamp = DateTimeOffset.UnixEpoch
        });
    }

    [Fact]
    public void DiscountFactor_PreservesCurveMaturityAndAllowsValuesAboveOne()
    {
        var discountFactor = new DiscountFactor
        {
            CurveName = "EUR-OIS",
            Currency = "EUR",
            Value = 1.01,
            MaturityDate = new DateOnly(2027, 1, 1),
            Timestamp = DateTimeOffset.UnixEpoch
        };

        Assert.Equal("EUR-OIS", discountFactor.CurveName);
        Assert.Equal("EUR", discountFactor.Currency);
        Assert.Equal(1.01, discountFactor.Value);
        Assert.Equal(new DateOnly(2027, 1, 1), discountFactor.MaturityDate);
    }

    #endregion
}
