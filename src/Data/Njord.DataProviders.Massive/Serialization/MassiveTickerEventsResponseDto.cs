using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveTickerEventsResponseDto : IMassiveApiResponseDto
{
    #region Response metadata

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("next_url")]
    public string? NextUrl { get; set; }

    [JsonPropertyName("results")]
    public MassiveTickerEventsResultDto? Results { get; set; }

    #endregion
}

internal sealed class MassiveTickerEventsResultDto
{
    #region Ticker events

    [JsonPropertyName("events")]
    public List<MassiveTickerEventDto>? Events { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    #endregion
}

internal sealed class MassiveTickerEventDto
{
    #region Ticker event

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("ticker_change")]
    public MassiveTickerChangeEventDto? TickerChange { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    #endregion
}

internal sealed class MassiveTickerChangeEventDto
{
    #region Ticker change

    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    #endregion
}
