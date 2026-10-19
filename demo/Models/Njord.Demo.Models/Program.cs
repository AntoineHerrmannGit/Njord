using Njord.Models.Instruments;
using Njord.Models.MarketData;

namespace Njord.Demo.Models;

internal static class Program
{
    #region Application

    private static void Main()
    {
        var observedAt = new DateTimeOffset(2025, 1, 15, 21, 0, 0, TimeSpan.Zero);
        var stock = new Stock
        {
            Ticker = "AAPL",
            Timestamp = observedAt
        };
        var dailyBar = new Ohlc
        {
            Ticker = stock.Ticker,
            Timestamp = observedAt,
            PeriodStart = observedAt,
            Interval = TimeSpan.FromDays(1),
            Open = 234.12m,
            High = 238.96m,
            Low = 233.44m,
            Close = 237.87m,
            Volume = 39_472_000m
        };

        Console.WriteLine($"Instrument : {stock.Ticker}, observation {stock.Timestamp:O}");
        Console.WriteLine(
            $"Barre OHLC : O={dailyBar.Open}, H={dailyBar.High}, L={dailyBar.Low}, C={dailyBar.Close}");
        Console.WriteLine($"Volume : {dailyBar.Volume:N0}; devise : {dailyBar.Currency ?? "non renseignée"}");
    }

    #endregion
}
