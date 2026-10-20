using System.Text.Json;
using Microsoft.Extensions.Options;
using Njord.Data.Referential.Configuration;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.Data.Referential.Persistence;
using Njord.Data.Referential.Services;

namespace Njord.Data.Referential.Tests;

public sealed class InstrumentReferentialRepositoryTests
{
    #region Primary JSON database

    [Fact]
    public async Task AddMappingsAsync_PersistsPlainMappingArrayAndCanBeReadByNewRepository()
    {
        await using var fixture = new ReferentialFixture();
        var mapping = CreateMapping(
            "SPX",
            "Massive",
            "I:SPX",
            NjordAssetClass.Index,
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31));

        await fixture.Referential.AddMappingsAsync([mapping]);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(fixture.DataFilePath));
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.False(document.RootElement[0].TryGetProperty("SchemaVersion", out _));
        Assert.Equal(mapping.Ticker, document.RootElement[0].GetProperty("Ticker").GetString());

        var reloadedReferential = fixture.CreateReferential();
        var reloaded = Assert.Single(await reloadedReferential.GetMappingsAsync());
        Assert.Equal(mapping.ProviderTicker, reloaded.ProviderTicker);
        Assert.Equal(mapping.AssetClass, reloaded.AssetClass);
    }

    [Fact]
    public async Task GetMappingsAsync_TreatsMissingFileAsEmptyAndFirstWriteCreatesIt()
    {
        await using var fixture = new ReferentialFixture();

        Assert.Empty(await fixture.Referential.GetMappingsAsync());
        Assert.False(File.Exists(fixture.DataFilePath));

        await fixture.Referential.AddMappingsAsync(
        [
            CreateMapping(
                "AAPL",
                "Massive",
                "AAPL",
                NjordAssetClass.Stock,
                DateTime.MinValue,
                DateTime.MaxValue)
        ]);

        Assert.True(File.Exists(fixture.DataFilePath));
    }

    [Fact]
    public async Task GetMappingsAsync_ThrowsActionableErrorForInvalidOrWrappedJson()
    {
        await using var fixture = new ReferentialFixture();
        Directory.CreateDirectory(Path.GetDirectoryName(fixture.DataFilePath)!);
        await File.WriteAllTextAsync(fixture.DataFilePath, """{"SchemaVersion":2,"Mappings":[]}""");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => fixture.Referential.GetMappingsAsync());

        Assert.Contains(fixture.DataFilePath, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddMappingsAsync_IsIdempotentForAnIdenticalMapping()
    {
        await using var fixture = new ReferentialFixture();
        var mapping = CreateMapping(
            "AAPL",
            "Massive",
            "AAPL",
            NjordAssetClass.Stock,
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31));

        await fixture.Referential.AddMappingsAsync([mapping]);
        await fixture.Referential.AddMappingsAsync([mapping]);

        Assert.Single(await fixture.Referential.GetMappingsAsync());
    }

    #endregion

    #region Mapping validity and resolution

    [Fact]
    public async Task ResolveAsync_ReturnsContiguousMappingsCoveringInclusiveQueryPeriod()
    {
        await using var fixture = new ReferentialFixture();
        var first = CreateMapping(
            "SPX",
            "Massive",
            "I:SPX",
            NjordAssetClass.Index,
            new DateTime(2025, 1, 1, 17, 0, 0),
            new DateTime(2025, 1, 15, 8, 0, 0));
        var second = CreateMapping(
            "SPX",
            "Massive",
            "I:SPX2",
            NjordAssetClass.Index,
            new DateTime(2025, 1, 16, 12, 0, 0),
            new DateTime(2025, 1, 31, 9, 0, 0));
        await fixture.Referential.AddMappingsAsync([first, second]);

        var resolved = await fixture.Referential.ResolveAsync(
            "spx",
            "massive",
            new DateOnly(2025, 1, 10),
            new DateOnly(2025, 1, 20));

        Assert.Equal(
            [first.ProviderTicker, second.ProviderTicker],
            resolved.Select(mapping => mapping.ProviderTicker));
    }

    [Fact]
    public async Task ResolveAsync_RejectsMissingTickerOrUncoveredDateRange()
    {
        await using var fixture = new ReferentialFixture();
        await fixture.Referential.AddMappingsAsync(
        [
            CreateMapping(
                "AAPL",
                "Massive",
                "AAPL",
                NjordAssetClass.Stock,
                new DateTime(2025, 1, 2),
                new DateTime(2025, 1, 10))
        ]);

        await Assert.ThrowsAsync<TickerReferenceResolutionException>(() =>
            fixture.Referential.ResolveAsync(
                "AAPL",
                "Massive",
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 1, 10)));
        await Assert.ThrowsAsync<TickerReferenceResolutionException>(() =>
            fixture.Referential.ResolveAsync(
                "MSFT",
                "Massive",
                new DateOnly(2025, 1, 2),
                new DateOnly(2025, 1, 10)));
    }

    [Fact]
    public async Task AddMappingsAsync_RejectsOverlapsForSameTickerAndProvider()
    {
        await using var fixture = new ReferentialFixture();
        await fixture.Referential.AddMappingsAsync(
        [
            CreateMapping(
                "AAPL",
                "Massive",
                "AAPL",
                NjordAssetClass.Stock,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 10))
        ]);

        await Assert.ThrowsAsync<TickerReferenceConflictException>(() =>
            fixture.Referential.AddMappingsAsync(
            [
                CreateMapping(
                    "AAPL",
                    "Massive",
                    "AAPL.N",
                    NjordAssetClass.Stock,
                    new DateTime(2025, 1, 10),
                    new DateTime(2025, 1, 20))
            ]));

        Assert.Single(await fixture.Referential.GetMappingsAsync());
    }

    [Fact]
    public async Task AddMappingsAsync_RejectsNestedOverlappingPeriods()
    {
        await using var fixture = new ReferentialFixture();

        await Assert.ThrowsAsync<TickerReferenceConflictException>(() =>
            fixture.Referential.AddMappingsAsync(
            [
                CreateMapping(
                    "AAPL",
                    "Massive",
                    "AAPL",
                    NjordAssetClass.Stock,
                    new DateTime(2025, 1, 1),
                    new DateTime(2025, 1, 20)),
                CreateMapping(
                    "AAPL",
                    "Massive",
                    "AAPL.OLD",
                    NjordAssetClass.Stock,
                    new DateTime(2025, 1, 2),
                    new DateTime(2025, 1, 3)),
                CreateMapping(
                    "AAPL",
                    "Massive",
                    "AAPL.NEW",
                    NjordAssetClass.Stock,
                    new DateTime(2025, 1, 10),
                    new DateTime(2025, 1, 30))
            ]));

        Assert.Empty(await fixture.Referential.GetMappingsAsync());
    }

    [Fact]
    public async Task AddMappingsAsync_AllowsAdjacentDatesAndOverlappingDatesAcrossProviders()
    {
        await using var fixture = new ReferentialFixture();
        var first = CreateMapping(
            "AAPL",
            "Massive",
            "AAPL",
            NjordAssetClass.Stock,
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 10));
        var adjacent = CreateMapping(
            "AAPL",
            "Massive",
            "AAPL.NEW",
            NjordAssetClass.Stock,
            new DateTime(2025, 1, 11),
            new DateTime(2025, 1, 20));
        var otherProvider = CreateMapping(
            "AAPL",
            "OtherProvider",
            "AAPL.US",
            NjordAssetClass.Stock,
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31));

        await fixture.Referential.AddMappingsAsync([first, adjacent, otherProvider]);

        Assert.Equal(3, (await fixture.Referential.GetMappingsAsync()).Count);
    }

    [Fact]
    public void TickerProviderMapping_DefaultsToUnboundedQuotationValidity()
    {
        var mapping = new TickerProviderMapping
        {
            Ticker = "AAPL",
            Provider = "Massive",
            ProviderTicker = "AAPL",
            AssetClass = NjordAssetClass.Stock
        };

        Assert.Equal(DateTime.MinValue, mapping.StartDate);
        Assert.Equal(DateTime.MaxValue, mapping.EndDate);
    }

    #endregion

    #region Test helpers

    private static TickerProviderMapping CreateMapping(
        string ticker,
        string provider,
        string providerTicker,
        NjordAssetClass assetClass,
        DateTime startDate,
        DateTime endDate) =>
        new()
        {
            Ticker = ticker,
            Provider = provider,
            ProviderTicker = providerTicker,
            AssetClass = assetClass,
            StartDate = startDate,
            EndDate = endDate
        };

    private sealed class ReferentialFixture : IAsyncDisposable
    {
        private readonly string _directoryPath =
            Path.Combine(Path.GetTempPath(), $"njord-referential-{Guid.NewGuid():N}");

        public string DataFilePath => Path.Combine(_directoryPath, "instruments.json");

        public InstrumentReferential Referential => CreateReferential();

        public InstrumentReferential CreateReferential()
        {
            var options = Options.Create(new ReferentialOptions
            {
                DataFilePath = DataFilePath
            });

            return new InstrumentReferential(
                new JsonInstrumentMappingRepository(options));
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_directoryPath))
                Directory.Delete(_directoryPath, recursive: true);

            return ValueTask.CompletedTask;
        }
    }

    #endregion
}
