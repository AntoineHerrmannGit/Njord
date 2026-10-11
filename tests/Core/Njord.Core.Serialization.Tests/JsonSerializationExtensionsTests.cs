using System.Text.Json;
using Njord.Core.Serialization;

namespace Njord.Core.Serialization.Tests;

public sealed class JsonSerializationExtensionsTests
{
    #region Enum conversion

    [Fact]
    public void Serialize_Enum_WritesTheEnumNameAsAString()
    {
        var json = TestStatus.Active.Serialize();

        Assert.Equal("\"Active\"", json);
    }

    [Fact]
    public void Déserialize_Enum_ReadsTheEnumNameFromAString()
    {
        var value = "\"Active\"".Déserialize<TestStatus>();

        Assert.Equal(TestStatus.Active, value);
    }

    [Fact]
    public void Déserialize_Enum_RejectsNumericValues()
    {
        Assert.Throws<JsonException>(() => "1".Déserialize<TestStatus>());
    }

    #endregion

    #region Value types

    [Fact]
    public void SerializeAndDéserialize_Struct_PreservesItsValues()
    {
        var original = new TestValue(42, TestStatus.Active);

        var result = original.Serialize().Déserialize<TestValue>();

        Assert.Equal(original, result);
    }

    [Fact]
    public void SerializeAndDéserialize_NullableStruct_PreservesNullAndValue()
    {
        TestValue? value = new TestValue(42, TestStatus.Active);
        TestValue? empty = null;

        var result = value.Serialize().Déserialize<TestValue?>();
        var nullResult = empty.Serialize().Déserialize<TestValue?>();

        Assert.Equal(value, result);
        Assert.Null(nullResult);
    }

    #endregion

    #region DateTime conversion

    [Fact]
    public void Serialize_DateTime_WritesRoundTripIso8601Format()
    {
        var value = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(6789);

        var json = value.Serialize();

        Assert.Equal("\"2025-01-02T03:04:05.0006789Z\"", json);
    }

    [Theory]
    [InlineData("\"2025-01-02T03:04:05Z\"", 2025, 1, 2, 3, 4, 5)]
    [InlineData("\"Thursday, 02 January 2025 03:04:05 GMT\"", 2025, 1, 2, 3, 4, 5)]
    public void Déserialize_DateTime_ReadsIsoAndTextualRepresentations(
        string json,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        var value = json.Déserialize<DateTime>();

        Assert.Equal(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc), value);
    }

    [Fact]
    public void Déserialize_DateTime_RejectsUnsupportedRepresentations()
    {
        Assert.Throws<JsonException>(() => "\"not a date\"".Déserialize<DateTime>());
    }

    [Theory]
    [InlineData("1735689600", false)]
    [InlineData("1735689600000", true)]
    [InlineData("-1000", false)]
    [InlineData("-1735689600000", true)]
    public void Déserialize_DateTime_ReadsUnixSecondsAndMilliseconds(string json, bool milliseconds)
    {
        var timestamp = long.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
        var expected = milliseconds
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
            : DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

        var value = json.Déserialize<DateTime>();

        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("999999999999999999999999")]
    [InlineData("253402300800000")]
    public void Déserialize_DateTime_RejectsNonIntegerOrOutOfRangeUnixTimestamps(string json)
    {
        Assert.Throws<JsonException>(() => json.Déserialize<DateTime>());
    }

    [Fact]
    public void Déserialize_NullableDateTime_ReadsUnixTimestamp()
    {
        var value = "1735689600000".Déserialize<DateTime?>();

        Assert.Equal(
            DateTimeOffset.FromUnixTimeMilliseconds(1_735_689_600_000).UtcDateTime,
            value);
    }

    [Fact]
    public void SerializeAndDéserialize_NullableDateTime_PreservesNullAndValue()
    {
        DateTime? value = new(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        DateTime? empty = null;

        var result = value.Serialize().Déserialize<DateTime?>();
        var nullResult = empty.Serialize().Déserialize<DateTime?>();

        Assert.Equal(value, result);
        Assert.Null(nullResult);
    }

    #endregion

    #region Test types

    private enum TestStatus
    {
        Inactive,
        Active
    }

    private readonly record struct TestValue(int Count, TestStatus Status);

    #endregion
}
