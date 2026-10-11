using System.Text;
using System.Text.Json;
using Njord.Core.Serialization;

namespace Njord.Core.Serialization.Tests;

public sealed class SystemTextJsonSerializerTests
{
    private readonly SystemTextJsonSerializer _serializer = new();

    [Fact]
    public void Serialize_UsesSharedWebDefaults()
    {
        var json = _serializer.Serialize(new SampleModel("Njord", 10));

        Assert.Equal("""{"name":"Njord","count":10}""", json);
    }

    [Fact]
    public void Deserialize_MapsJsonToModel()
    {
        var model = _serializer.Deserialize<SampleModel>( """{"name":"Njord","count":10}""");

        Assert.Equal(new SampleModel("Njord", 10), model);
    }

    [Fact]
    public void Deserialize_ReportsMalformedJsonWithoutEmbeddingPayload()
    {
        const string secretPayload = """{"name":"private-value","count":""";

        var exception = Assert.Throws<JsonSerializationException>(
            () => _serializer.Deserialize<SampleModel>(secretPayload));

        Assert.Equal("deserialization", exception.Operation);
        Assert.Equal(typeof(SampleModel), exception.ModelType);
        Assert.IsType<JsonException>(exception.InnerException);
        Assert.DoesNotContain("private-value", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_RejectsJsonNull()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => _serializer.Deserialize<SampleModel>("null"));

        Assert.Equal("deserialization", exception.Operation);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task SerializeAsyncAndDeserializeAsync_RoundTripThroughStream()
    {
        await using var stream = new MemoryStream();
        var source = new SampleModel("Njord", 10);

        await _serializer.SerializeAsync(stream, source);
        stream.Position = 0;
        var result = await _serializer.DeserializeAsync<SampleModel>(stream);

        Assert.Equal(source, result);
    }

    [Fact]
    public async Task DeserializeAsync_PropagatesCancellation()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"name":"Njord","count":10}"""));
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _serializer.DeserializeAsync<SampleModel>(stream, cancellationSource.Token));
    }

    [Fact]
    public void Constructor_CopiesSuppliedOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var serializer = new SystemTextJsonSerializer(options);
        options.PropertyNamingPolicy = null;

        Assert.Equal("""{"name":"Njord","count":10}""", serializer.Serialize(new SampleModel("Njord", 10)));
    }

    [Fact]
    public void JsonElementReader_ReadsRequiredAndOptionalValues()
    {
        using var document = JsonDocument.Parse("""{"symbol":"AAPL","volume":123,"active":true,"missing":null}""");

        Assert.Equal("AAPL", JsonElementReader.ReadOptionalString(document.RootElement, "symbol"));
        Assert.Equal(123L, JsonElementReader.ReadOptionalInt64(document.RootElement, "volume"));
        Assert.True(JsonElementReader.ReadOptionalBoolean(document.RootElement, "active"));
        Assert.Null(JsonElementReader.ReadOptionalString(document.RootElement, "missing"));
        Assert.Throws<JsonSerializationException>(
            () => JsonElementReader.GetRequiredProperty(document.RootElement, "absent"));
    }

    [Fact]
    public void JsonElementReader_RejectsInvalidPrimitiveTypesWithFieldPath()
    {
        using var document = JsonDocument.Parse("""{"volume":"unknown"}""");

        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonElementReader.ReadOptionalInt64(document.RootElement, "volume"));

        Assert.Equal("volume", exception.Path);
        Assert.Contains("volume", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonElementReader_ReportsOutOfRangeUnixTimestamp()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonElementReader.ReadUnixTimestamp(long.MaxValue, "timestamp"));

        Assert.Equal("timestamp", exception.Path);
        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    private sealed record SampleModel(string Name, int Count);
}
