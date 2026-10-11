namespace Njord.Core.Serialization;

/// <summary>Represents a JSON serialization or deserialization failure.</summary>
public sealed class JsonSerializationException : Exception
{
    /// <summary>Creates a serialization exception for the specified operation and model type.</summary>
    public JsonSerializationException(
        string operation,
        Type modelType,
        Exception? innerException = null,
        string? path = null)
        : base(CreateMessage(operation, modelType, path), innerException)
    {
        Operation = operation;
        ModelType = modelType;
        Path = path;
    }

    /// <summary>Gets the serialization operation that failed.</summary>
    public string Operation { get; }

    /// <summary>Gets the model type involved in the failed operation.</summary>
    public Type ModelType { get; }

    /// <summary>Gets the JSON property path involved in the failed operation, when available.</summary>
    public string? Path { get; }

    private static string CreateMessage(string operation, Type modelType, string? path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(modelType);
        var pathDescription = string.IsNullOrWhiteSpace(path) ? string.Empty : $" at '{path}'";
        return $"JSON {operation} failed for model type '{modelType.FullName}'{pathDescription}.";
    }
}
