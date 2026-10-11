using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveAggregateResponseDto
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

    [JsonPropertyName("adjusted")]
    public bool? Adjusted { get; set; }

    [JsonPropertyName("results")]
    public List<MassiveAggregateDto>? Results { get; set; }

    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    #endregion
}
