using System.Text.Json.Serialization;

namespace Njord.DataProviders.YahooFinance.Serialization;

internal sealed class YahooFinanceChartResponseDto
{
    #region Chart response

    [JsonPropertyName("chart")]
    public YahooFinanceChartDto? Chart { get; set; }

    #endregion
}

internal sealed class YahooFinanceChartDto
{
    #region Chart result

    [JsonPropertyName("result")]
    public List<YahooFinanceChartResultDto>? Result { get; set; }

    [JsonPropertyName("error")]
    public YahooFinanceChartErrorDto? Error { get; set; }

    #endregion
}

internal sealed class YahooFinanceChartErrorDto
{
    #region Error details

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    #endregion
}

internal sealed class YahooFinanceChartResultDto
{
    #region Chart metadata and values

    [JsonPropertyName("meta")]
    public YahooFinanceChartMetadataDto? Meta { get; set; }

    [JsonPropertyName("timestamp")]
    public List<long>? Timestamps { get; set; }

    [JsonPropertyName("events")]
    public YahooFinanceChartEventsDto? Events { get; set; }

    [JsonPropertyName("indicators")]
    public YahooFinanceChartIndicatorsDto? Indicators { get; set; }

    #endregion
}

internal sealed class YahooFinanceChartMetadataDto
{
    #region Market metadata

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("exchangeTimezoneName")]
    public string? ExchangeTimeZoneName { get; set; }

    #endregion
}

internal sealed class YahooFinanceChartIndicatorsDto
{
    #region Indicators

    [JsonPropertyName("quote")]
    public List<YahooFinanceQuoteDto>? Quote { get; set; }

    #endregion
}

internal sealed class YahooFinanceChartEventsDto
{
    #region Corporate actions

    [JsonPropertyName("dividends")]
    public Dictionary<string, YahooFinanceDividendEventDto>? Dividends { get; set; }

    [JsonPropertyName("splits")]
    public Dictionary<string, YahooFinanceSplitEventDto>? Splits { get; set; }

    #endregion
}

internal sealed class YahooFinanceDividendEventDto
{
    #region Dividend event

    [JsonPropertyName("date")]
    public long? Date { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    #endregion
}

internal sealed class YahooFinanceSplitEventDto
{
    #region Split event

    [JsonPropertyName("date")]
    public long? Date { get; set; }

    [JsonPropertyName("numerator")]
    public decimal? Numerator { get; set; }

    [JsonPropertyName("denominator")]
    public decimal? Denominator { get; set; }

    [JsonPropertyName("splitRatio")]
    public string? SplitRatio { get; set; }

    #endregion
}

internal sealed class YahooFinanceQuoteDto
{
    #region OHLC values

    [JsonPropertyName("open")]
    public List<decimal?>? Open { get; set; }

    [JsonPropertyName("high")]
    public List<decimal?>? High { get; set; }

    [JsonPropertyName("low")]
    public List<decimal?>? Low { get; set; }

    [JsonPropertyName("close")]
    public List<decimal?>? Close { get; set; }

    [JsonPropertyName("volume")]
    public List<decimal?>? Volume { get; set; }

    #endregion
}
