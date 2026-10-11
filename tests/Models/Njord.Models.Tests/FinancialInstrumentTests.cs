using Njord.Models.Instruments;
using Njord.Models.MarketData;

namespace Njord.Models.Tests;

public sealed class FinancialInstrumentTests
{
    #region Shared instrument contracts

    [Fact]
    public void FinancialInstruments_ExposeTickerAndTimestampThroughCommonInterface()
    {
        var timestamp = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        IFinancialInstrument[] instruments =
        [
            new Stock { Ticker = "AAPL", Timestamp = timestamp },
            new Future { Ticker = "ES", Timestamp = timestamp },
            new Option
            {
                Ticker = "AAPL-C",
                Timestamp = timestamp,
                UnderlyingTicker = "AAPL",
                ExpirationDate = new DateOnly(2025, 6, 20),
                Type = OptionType.Call,
                Strike = 200m
            },
            new MarketIndex { Ticker = "SPX", Timestamp = timestamp },
            new Forex
            {
                Ticker = "EURUSD",
                Timestamp = timestamp,
                BaseCurrency = "EUR",
                QuoteCurrency = "USD"
            },
            new Swap
            {
                Ticker = "IRS-1",
                Timestamp = timestamp,
                Currency = "USD",
                Notional = 1_000_000m
            },
            new Bond
            {
                Ticker = "US-T",
                Timestamp = timestamp,
                Currency = "USD",
                FaceValue = 1_000m
            },
            new Repo
            {
                Ticker = "REPO-1",
                Timestamp = timestamp,
                Currency = "USD",
                CashAmount = 1_000_000m
            }
        ];

        Assert.All(instruments, instrument => Assert.Equal(timestamp, instrument.Timestamp));
        Assert.Equal(
            new[] { "AAPL", "ES", "AAPL-C", "SPX", "EURUSD", "IRS-1", "US-T", "REPO-1" },
            instruments.Select(instrument => instrument.Ticker));
    }

    [Fact]
    public void FinancialInstrument_RejectsBlankTicker()
    {
        Assert.Throws<ArgumentException>(() => new Stock
        {
            Ticker = " ",
            Timestamp = DateTimeOffset.UnixEpoch
        });
    }

    #endregion

    #region Instrument terms

    [Fact]
    public void Option_PreservesItsContractTerms()
    {
        var expiration = new DateOnly(2025, 6, 20);
        var option = new Option
        {
            Ticker = "AAPL-C-200",
            Timestamp = DateTimeOffset.UnixEpoch,
            UnderlyingTicker = "AAPL",
            ExpirationDate = expiration,
            Type = OptionType.Call,
            Strike = 200m
        };

        Assert.Equal("AAPL", option.UnderlyingTicker);
        Assert.Equal(expiration, option.ExpirationDate);
        Assert.Equal(OptionType.Call, option.Type);
        Assert.Equal(200m, option.Strike);
    }

    [Fact]
    public void Future_PreservesOptionalContractDetails()
    {
        var expiration = new DateOnly(2025, 6, 20);
        var future = new Future
        {
            Ticker = "ESM5",
            Timestamp = DateTimeOffset.UnixEpoch,
            ExpirationDate = expiration,
            UnderlyingTicker = "ES",
            ContractSize = 50m
        };

        Assert.Equal(expiration, future.ExpirationDate);
        Assert.Equal("ES", future.UnderlyingTicker);
        Assert.Equal(50m, future.ContractSize);
    }

    [Fact]
    public void Forex_PreservesBaseAndQuoteCurrencies()
    {
        var forex = new Forex
        {
            Ticker = "EURUSD",
            Timestamp = DateTimeOffset.UnixEpoch,
            BaseCurrency = "EUR",
            QuoteCurrency = "USD"
        };

        Assert.Equal("EUR", forex.BaseCurrency);
        Assert.Equal("USD", forex.QuoteCurrency);
    }

    [Fact]
    public void SwapAndBond_StoreRatesAsDecimalFractions()
    {
        var swap = new Swap
        {
            Ticker = "IRS-1",
            Timestamp = DateTimeOffset.UnixEpoch,
            Currency = "USD",
            Notional = 1_000_000m,
            FixedRate = 0.05m,
            FloatingRateIndex = "SOFR"
        };
        var bond = new Bond
        {
            Ticker = "UST",
            Timestamp = DateTimeOffset.UnixEpoch,
            Currency = "USD",
            FaceValue = 1_000m,
            CouponRate = 0.04m,
            CouponPaymentsPerYear = 2
        };

        Assert.Equal(0.05m, swap.FixedRate);
        Assert.Equal("SOFR", swap.FloatingRateIndex);
        Assert.Equal(0.04m, bond.CouponRate);
        Assert.Equal(2, bond.CouponPaymentsPerYear);
    }

    [Fact]
    public void Repo_PreservesCashCollateralAndRateTerms()
    {
        var repo = new Repo
        {
            Ticker = "REPO-1",
            Timestamp = DateTimeOffset.UnixEpoch,
            Currency = "USD",
            CashAmount = 1_000_000m,
            CollateralTicker = "UST",
            StartDate = new DateOnly(2025, 1, 2),
            MaturityDate = new DateOnly(2025, 1, 3),
            RepoRate = 0.05m
        };

        Assert.Equal(1_000_000m, repo.CashAmount);
        Assert.Equal("UST", repo.CollateralTicker);
        Assert.Equal(new DateOnly(2025, 1, 2), repo.StartDate);
        Assert.Equal(new DateOnly(2025, 1, 3), repo.MaturityDate);
        Assert.Equal(0.05m, repo.RepoRate);
    }

    [Fact]
    public void Rate_StoresValuesAsDecimalFraction()
    {
        var rate = new Rate
        {
            Name = "SOFR",
            Value = 0.05m,
            Timestamp = DateTimeOffset.UnixEpoch,
            Currency = "USD"
        };

        Assert.Equal(0.05m, rate.Value);
        Assert.Equal("USD", rate.Currency);
    }

    [Fact]
    public void YieldCurve_CopiesInputPoints()
    {
        var points = new List<YieldCurvePoint>
        {
            new() { MaturityDate = new DateOnly(2026, 1, 1), Rate = 0.04m }
        };
        var curve = new YieldCurve
        {
            Name = "USD-GOV",
            Currency = "USD",
            Timestamp = DateTimeOffset.UnixEpoch,
            Points = points
        };
        points.Add(new YieldCurvePoint { MaturityDate = new DateOnly(2027, 1, 1), Rate = 0.045m });

        Assert.Single(curve.Points);
        Assert.Equal(0.04m, curve.Points[0].Rate);
    }

    [Fact]
    public void YieldCurve_RejectsNullPoints()
    {
        Assert.Throws<ArgumentNullException>(
            () => new YieldCurve
            {
                Name = "USD-GOV",
                Currency = "USD",
                Timestamp = DateTimeOffset.UnixEpoch,
                Points = null!
            });
    }

    [Fact]
    public void RequiredStringProperties_RejectBlankValues()
    {
        Assert.Throws<ArgumentException>(() => new Option
        {
            Ticker = "OPT",
            Timestamp = DateTimeOffset.UnixEpoch,
            UnderlyingTicker = " ",
            ExpirationDate = new DateOnly(2025, 6, 20),
            Type = OptionType.Call,
            Strike = 10m
        });
        Assert.Throws<ArgumentException>(() => new Forex
        {
            Ticker = "EURUSD",
            Timestamp = DateTimeOffset.UnixEpoch,
            BaseCurrency = "EUR",
            QuoteCurrency = " "
        });
    }

    #endregion
}
