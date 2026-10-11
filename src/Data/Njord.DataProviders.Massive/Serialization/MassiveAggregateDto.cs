using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveAggregateDto
{
    #region Standard aggregate fields

    [JsonPropertyName("o")]
    public decimal? Open { get; set; }

    [JsonPropertyName("h")]
    public decimal? High { get; set; }

    [JsonPropertyName("l")]
    public decimal? Low { get; set; }

    [JsonPropertyName("c")]
    public decimal? Close { get; set; }

    [JsonPropertyName("t")]
    public long? TimestampMilliseconds { get; set; }

    [JsonPropertyName("v")]
    public decimal? Volume { get; set; }

    [JsonPropertyName("vw")]
    public decimal? VolumeWeightedAveragePrice { get; set; }

    [JsonPropertyName("n")]
    public long? Transactions { get; set; }

    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    #endregion

    #region Futures aggregate fields

    [JsonPropertyName("open")]
    public decimal? FuturesOpen { get; set; }

    [JsonPropertyName("high")]
    public decimal? FuturesHigh { get; set; }

    [JsonPropertyName("low")]
    public decimal? FuturesLow { get; set; }

    [JsonPropertyName("close")]
    public decimal? FuturesClose { get; set; }

    [JsonPropertyName("window_start")]
    public long? WindowStartNanoseconds { get; set; }

    [JsonPropertyName("volume")]
    public decimal? FuturesVolume { get; set; }

    [JsonPropertyName("transactions")]
    public long? FuturesTransactions { get; set; }

    #endregion
}
