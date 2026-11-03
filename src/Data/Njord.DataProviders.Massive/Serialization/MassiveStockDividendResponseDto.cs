using System.Text.Json.Serialization;

namespace Njord.DataProviders.Massive.Serialization;

internal sealed class MassiveStockDividendResponseDto : IMassivePagedResponseDto<MassiveStockDividendDto>
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
    public List<MassiveStockDividendDto>? Results { get; set; }

    #endregion
}

internal sealed class MassiveStockDividendDto
{
    #region Dividend event

    [JsonPropertyName("cash_amount")]
    public decimal? CashAmount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("declaration_date")]
    public string? DeclarationDate { get; set; }

    [JsonPropertyName("distribution_type")]
    public string? DistributionType { get; set; }

    [JsonPropertyName("ex_dividend_date")]
    public string? ExDividendDate { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("pay_date")]
    public string? PayDate { get; set; }

    [JsonPropertyName("record_date")]
    public string? RecordDate { get; set; }

    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    #endregion
}
