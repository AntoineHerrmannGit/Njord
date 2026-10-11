namespace Njord.Core.Serialization;

/// <summary>Provides the solution-wide JSON serialization contract.</summary>
public interface IJsonSerializer
{
    /// <summary>Serializes a model to JSON text.</summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="value">The model instance.</param>
    /// <returns>The serialized JSON.</returns>
    /// <exception cref="JsonSerializationException">Serialization fails for the model.</exception>
    string Serialize<T>(T value);

    /// <summary>Deserializes JSON text into a model instance.</summary>
    /// <typeparam name="T">The target model type.</typeparam>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <returns>The deserialized model.</returns>
    /// <exception cref="JsonSerializationException">The JSON is invalid or represents null.</exception>
    T Deserialize<T>(string json);

    /// <summary>Serializes a model as JSON to a stream.</summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="stream">The destination stream.</param>
    /// <param name="value">The model instance.</param>
    /// <param name="cancellationToken">Token used to cancel serialization.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="JsonSerializationException">Serialization fails for the model.</exception>
    Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);

    /// <summary>Deserializes JSON from a stream into a model instance.</summary>
    /// <typeparam name="T">The target model type.</typeparam>
    /// <param name="stream">The source stream.</param>
    /// <param name="cancellationToken">Token used to cancel deserialization.</param>
    /// <returns>The deserialized model.</returns>
    /// <exception cref="JsonSerializationException">The JSON is invalid or represents null.</exception>
    Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
}
