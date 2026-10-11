using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Njord.Core.Serialization;

internal sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    #region JSON conversion

    private const long MillisecondTimestampThreshold = 100_000_000_000;

    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return ReadUnixTimestamp(ref reader);

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("A DateTime JSON value must be a string or a Unix timestamp number.");

        var value = reader.GetString();
        if (value is null)
            throw new JsonException("A non-nullable DateTime value cannot be null.");

        if (reader.TryGetDateTime(out var isoDateTime))
            return isoDateTime;

        if (DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
            out var invariantDateTime))
            return invariantDateTime;

        if (DateTime.TryParse(
            value,
            CultureInfo.CurrentCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
            out var currentCultureDateTime))
            return currentCultureDateTime;

        throw new JsonException($"The value '{value}' is not a supported DateTime representation.");
    }

    private static DateTime ReadUnixTimestamp(ref Utf8JsonReader reader)
    {
        if (!reader.TryGetInt64(out var timestamp))
            throw new JsonException("A Unix timestamp must be an integer number.");

        try
        {
            return timestamp >= MillisecondTimestampThreshold ||
                   timestamp <= -MillisecondTimestampThreshold
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
                : DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new JsonException("The Unix timestamp is outside the supported DateTime range.", exception);
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));

    #endregion
}
