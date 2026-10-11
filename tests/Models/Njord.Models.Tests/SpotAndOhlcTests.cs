using Njord.Models.MarketData;

namespace Njord.Models.Tests;

public sealed class SpotAndOhlcTests
{
    #region Spot

    [Fact]
    public void Spot_PreservesPriceCurrencyAndObservationTimestamp()
    {
        var timestamp = new DateTimeOffset(2026, 10, 11, 11, 15, 0, TimeSpan.FromHours(2));
        var spot = new Spot
        {
            Ticker = "EURUSD",
            Price = 1.16235m,
            Currency = "USD",
            Timestamp = timestamp
        };

        Assert.Equal("EURUSD", spot.Ticker);
        Assert.Equal(1.16235m, spot.Price);
        Assert.Equal("USD", spot.Currency);
        Assert.Equal(timestamp, spot.Timestamp);
    }

    [Fact]
    public void Spot_RejectsBlankTickerAndCurrency()
    {
        Assert.Throws<ArgumentException>(() => new Spot
        {
            Ticker = " ",
            Price = 1m,
            Currency = "USD",
            Timestamp = DateTimeOffset.UnixEpoch
        });
        Assert.Throws<ArgumentException>(() => new Spot
        {
            Ticker = "EURUSD",
            Price = 1m,
            Currency = " ",
            Timestamp = DateTimeOffset.UnixEpoch
        });
    }

    #endregion

    #region OHLC

    [Fact]
    public void Ohlc_PreservesPricesAndOptionalPeriodMetadata()
    {
        var timestamp = new DateTimeOffset(2026, 10, 11, 11, 15, 0, TimeSpan.FromHours(2));
        var start = timestamp.AddMinutes(-1);
        var ohlc = new Ohlc
        {
            Ticker = "AAPL",
            Open = 250.10m,
            High = 252.40m,
            Low = 249.85m,
            Close = 251.75m,
            Currency = "USD",
            Timestamp = timestamp,
            Interval = TimeSpan.FromMinutes(1),
            PeriodStart = start,
            PeriodEnd = timestamp
        };

        Assert.Equal("AAPL", ohlc.Ticker);
        Assert.Equal(250.10m, ohlc.Open);
        Assert.Equal(252.40m, ohlc.High);
        Assert.Equal(249.85m, ohlc.Low);
        Assert.Equal(251.75m, ohlc.Close);
        Assert.Equal("USD", ohlc.Currency);
        Assert.Equal(timestamp, ohlc.Timestamp);
        Assert.Equal(TimeSpan.FromMinutes(1), ohlc.Interval);
        Assert.Equal(start, ohlc.PeriodStart);
        Assert.Equal(timestamp, ohlc.PeriodEnd);
    }

    [Fact]
    public void Ohlc_AllowsMissingPeriodMetadata()
    {
        var ohlc = new Ohlc
        {
            Ticker = "AAPL",
            Open = 250m,
            High = 252m,
            Low = 249m,
            Close = 251m,
            Currency = "USD",
            Timestamp = DateTimeOffset.UnixEpoch
        };

        Assert.Null(ohlc.Interval);
        Assert.Null(ohlc.PeriodStart);
        Assert.Null(ohlc.PeriodEnd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ohlc_RejectsNonPositiveInterval(int intervalMinutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ohlc
        {
            Ticker = "AAPL",
            Open = 250m,
            High = 252m,
            Low = 249m,
            Close = 251m,
            Currency = "USD",
            Timestamp = DateTimeOffset.UnixEpoch,
            Interval = TimeSpan.FromMinutes(intervalMinutes)
        });
    }

    #endregion
}
