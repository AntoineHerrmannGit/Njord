using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveErrorResponseDto : IMassiveApiResponseDto
{
    #region Error response metadata

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

    #endregion
}
