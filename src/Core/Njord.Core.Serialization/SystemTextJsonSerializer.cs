using System.Text.Json;

namespace Njord.Core.Serialization;

/// <summary>Solution-wide JSON serializer backed by <see cref="JsonSerializer"/>.</summary>
public sealed class SystemTextJsonSerializer : IJsonSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>Creates a serializer using web-oriented JSON defaults or a copy of supplied options.</summary>
    /// <param name="options">Optional JSON options. The instance copies this value and does not mutate it.</param>
    public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options is null
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            : new JsonSerializerOptions(options);
    }

    /// <inheritdoc />
    public string Serialize<T>(T value)
    {
        try
        {
            return JsonSerializer.Serialize(value, _options);
        }
        catch (JsonException exception)
        {
            throw new JsonSerializationException("serialization", typeof(T), exception);
        }
    }

    /// <inheritdoc />
    public T Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonSerializer.Deserialize<T>(json, _options)
                ?? throw new JsonSerializationException("deserialization", typeof(T));
        }
        catch (JsonException exception)
        {
            throw new JsonSerializationException("deserialization", typeof(T), exception);
        }
    }

    /// <inheritdoc />
    public async Task SerializeAsync<T>(
        Stream stream,
        T value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            await JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new JsonSerializationException("serialization", typeof(T), exception);
        }
    }

    /// <inheritdoc />
    public async Task<T> DeserializeAsync<T>(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken)
                       .ConfigureAwait(false)
                   ?? throw new JsonSerializationException("deserialization", typeof(T));
        }
        catch (JsonException exception)
        {
            throw new JsonSerializationException("deserialization", typeof(T), exception);
        }
    }
}
