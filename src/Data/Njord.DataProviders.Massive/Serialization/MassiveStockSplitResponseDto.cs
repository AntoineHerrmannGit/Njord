using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveStockSplitResponseDto : IMassivePagedResponseDto<MassiveStockSplitDto>
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
    public List<MassiveStockSplitDto>? Results { get; set; }

    #endregion
}

internal sealed class MassiveStockSplitDto
{
    #region Split event

    [JsonPropertyName("adjustment_type")]
    public string? AdjustmentType { get; set; }

    [JsonPropertyName("execution_date")]
    public string? ExecutionDate { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("split_from")]
    public decimal? SplitFrom { get; set; }

    [JsonPropertyName("split_to")]
    public decimal? SplitTo { get; set; }

    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    #endregion
}
