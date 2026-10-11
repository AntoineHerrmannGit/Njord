using System.Text.Json;

namespace Njord.Core.Serialization;

/// <summary>Reads common JSON object properties and scalar values with consistent validation.</summary>
public static class JsonElementReader
{
    /// <summary>Gets a non-null property from a JSON object.</summary>
    /// <param name="element">The JSON object to inspect.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="JsonSerializationException">The object or property is missing or invalid.</exception>
    public static JsonElement GetRequiredProperty(JsonElement element, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(name, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw ReadError(name);
        }

        return property;
    }

    /// <summary>Reads an optional string property. Missing and null values return null.</summary>
    public static string? ReadOptionalString(JsonElement element, string name)
    {
        var value = GetOptionalProperty(element, name);
        if (value is null)
        {
            return null;
        }

        return value.Value.ValueKind == JsonValueKind.String
            ? value.Value.GetString()
            : throw ReadError(name);
    }

    /// <summary>Reads an optional decimal number property. Missing and null values return null.</summary>
    public static decimal? ReadOptionalDecimal(JsonElement element, string name)
    {
        var value = GetOptionalProperty(element, name);
        if (value is null)
        {
            return null;
        }

        return value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDecimal(out var result)
            ? result
            : throw ReadError(name);
    }

    /// <summary>Reads an optional 64-bit integer property. Missing and null values return null.</summary>
    public static long? ReadOptionalInt64(JsonElement element, string name)
    {
        var value = GetOptionalProperty(element, name);
        if (value is null)
        {
            return null;
        }

        return value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt64(out var result)
            ? result
            : throw ReadError(name);
    }

    /// <summary>Reads an optional Boolean property. Missing and null values return null.</summary>
    public static bool? ReadOptionalBoolean(JsonElement element, string name)
    {
        var value = GetOptionalProperty(element, name);
        if (value is null)
        {
            return null;
        }

        return value.Value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.Value.GetBoolean()
            : throw ReadError(name);
    }

    /// <summary>Converts Unix seconds to UTC and reports out-of-range values as JSON data errors.</summary>
    public static DateTimeOffset ReadUnixTimestamp(long timestamp, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new JsonSerializationException(
                "timestamp reading",
                typeof(DateTimeOffset),
                exception,
                path);
        }
    }

    private static JsonElement? GetOptionalProperty(JsonElement element, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw ReadError(name);
        }

        if (!element.TryGetProperty(name, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value;
    }

    private static JsonSerializationException ReadError(string path) =>
        new("property reading", typeof(JsonElement), path: path);
}
