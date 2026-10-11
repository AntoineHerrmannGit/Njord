using System.Text.Json;
using System.Text.Json.Serialization;

namespace Njord.Core.Serialization;

/// <summary>Provides shared JSON serialization and deserialization extensions.</summary>
public static class JsonSerializationExtensions
{
    #region JSON operations

    /// <summary>Serializes an object to JSON using the solution-wide JSON settings.</summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The JSON representation of the object.</returns>
    public static string Serialize<T>(this T obj) =>
        JsonSerializer.Serialize(obj, SerializerOptions);

    /// <summary>Deserializes JSON using the solution-wide JSON settings.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <returns>The deserialized value, or <see langword="null"/> when the JSON value is null.</returns>
    public static T? Déserialize<T>(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    #endregion

    #region Serializer configuration

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        options.Converters.Add(new FlexibleDateTimeConverter());
        return options;
    }

    #endregion
}
