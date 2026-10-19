using Njord.Core.Serialization;

namespace Njord.Demo.Serialization;

internal static class Program
{
    #region Application

    private static void Main()
    {
        var snapshot = new MarketSnapshot
        {
            Ticker = "AAPL",
            ObservedAt = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc),
            Status = MarketStatus.Closed,
            LastPrice = 237.87m
        };

        var json = snapshot.Serialize();
        var restored = json.Déserialize<MarketSnapshot>()
            ?? throw new InvalidOperationException("The JSON payload could not be deserialized.");

        Console.WriteLine("JSON produit par Njord.Core.Serialization :");
        Console.WriteLine(json);
        Console.WriteLine();
        Console.WriteLine($"Ticker restauré : {restored.Ticker}");
        Console.WriteLine($"Statut enum restauré : {restored.Status}");
        Console.WriteLine($"Date UTC restaurée : {restored.ObservedAt:O}");
        Console.WriteLine($"Prix restauré : {restored.LastPrice}");
    }

    #endregion

    #region Demo data

    private sealed record MarketSnapshot
    {
        public required string Ticker { get; init; }

        public required DateTime ObservedAt { get; init; }

        public required MarketStatus Status { get; init; }

        public required decimal LastPrice { get; init; }
    }

    private enum MarketStatus
    {
        Open,
        Closed
    }

    #endregion
}
