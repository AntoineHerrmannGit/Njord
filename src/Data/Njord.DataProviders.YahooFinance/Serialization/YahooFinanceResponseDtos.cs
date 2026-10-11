using System.Text.Json;
using System.Text.Json.Serialization;

namespace Njord.DataProviders.YahooFinance.Serialization;

internal sealed record YahooFinanceChartResponse(
    [property: JsonPropertyName("chart")] JsonElement Chart);

internal sealed record YahooFinanceOptionChainResponse(
    [property: JsonPropertyName("optionChain")] JsonElement OptionChain);
