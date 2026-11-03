using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveStockIpoResponseDto : IMassivePagedResponseDto<MassiveStockIpoDto>
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
    public List<MassiveStockIpoDto>? Results { get; set; }

    #endregion
}

internal sealed class MassiveStockIpoDto
{
    #region IPO event

    [JsonPropertyName("announced_date")]
    public string? AnnouncedDate { get; set; }

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("ipo_status")]
    public string? IpoStatus { get; set; }

    [JsonPropertyName("issuer_name")]
    public string? IssuerName { get; set; }

    [JsonPropertyName("listing_date")]
    public string? ListingDate { get; set; }

    [JsonPropertyName("primary_exchange")]
    public string? PrimaryExchange { get; set; }

    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    #endregion
}
