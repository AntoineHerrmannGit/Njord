using System.Text.Json;
using Njord.Core.Serialization;
using Njord.MarketData;

namespace Njord.DataProviders.YahooFinance.Serialization;

internal static class YahooFinanceJsonReader
{
    public static JsonElement GetRequiredProperty(JsonElement element, string name) =>
        ReadJson(() => JsonElementReader.GetRequiredProperty(element, name), name);

    public static string? ReadOptionalString(JsonElement element, string name) =>
        ReadJson(() => JsonElementReader.ReadOptionalString(element, name), name);

    public static decimal? ReadOptionalDecimal(JsonElement element, string name) =>
        ReadJson(() => JsonElementReader.ReadOptionalDecimal(element, name), name);

    public static long? ReadOptionalInt64(JsonElement element, string name) =>
        ReadJson(() => JsonElementReader.ReadOptionalInt64(element, name), name);

    public static bool? ReadOptionalBoolean(JsonElement element, string name) =>
        ReadJson(() => JsonElementReader.ReadOptionalBoolean(element, name), name);

    public static DateTimeOffset ParseTimestamp(long timestamp) =>
        ReadJson(
            () => JsonElementReader.ReadUnixTimestamp(timestamp, "timestamp"),
            "timestamp");

    public static void ThrowIfApiError(JsonElement response)
    {
        if (response.ValueKind != JsonValueKind.Object ||
            !response.TryGetProperty("error", out var error) ||
            error.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        var code = ReadOptionalString(error, "code") ?? "Unknown";
        var description = ReadOptionalString(error, "description") ?? "No error description was supplied.";
        var statusCode = string.Equals(code, "Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(code, "Forbidden", StringComparison.OrdinalIgnoreCase)
            ? System.Net.HttpStatusCode.Unauthorized
            : (System.Net.HttpStatusCode?)null;
        throw new MarketDataProviderException(
            "YahooFinance",
            $"Yahoo Finance API error ({code}): {description}",
            statusCode);
    }

    public static bool IsAuthorizationError(JsonElement response)
    {
        if (response.ValueKind != JsonValueKind.Object ||
            !response.TryGetProperty("error", out var error) ||
            error.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var code = ReadOptionalString(error, "code");
        var description = ReadOptionalString(error, "description");
        return string.Equals(code, "Unauthorized", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "Forbidden", StringComparison.OrdinalIgnoreCase) ||
               (description?.Contains("crumb", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public static MarketDataProviderException InvalidResponse(string message, Exception? innerException = null) =>
        new("YahooFinance", message, innerException: innerException);

    private static T ReadJson<T>(Func<T> read, string fieldName)
    {
        try
        {
            return read();
        }
        catch (JsonSerializationException exception)
        {
            var pathDescription = exception.Path is null ? string.Empty : $" at '{exception.Path}'";
            throw InvalidResponse(
                $"Yahoo Finance returned invalid JSON data for '{fieldName}'{pathDescription}.",
                exception);
        }
    }
}
