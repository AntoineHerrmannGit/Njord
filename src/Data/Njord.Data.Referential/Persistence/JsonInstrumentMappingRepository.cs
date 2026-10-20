using System.Text.Json;
using Microsoft.Extensions.Options;
using Njord.Core.Serialization;
using Njord.Data.Referential.Configuration;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.Data.Referential.Validation;

namespace Njord.Data.Referential.Persistence;

internal sealed class JsonInstrumentMappingRepository : IInstrumentMappingRepository
{
    #region Dependencies

    private readonly string _dataFilePath;

    public JsonInstrumentMappingRepository(IOptions<ReferentialOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _dataFilePath = Path.GetFullPath(options.Value.DataFilePath);
    }

    #endregion

    #region Mapping persistence

    public async Task AddMappingsAsync(
        IReadOnlyCollection<TickerProviderMapping> mappings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        if (mappings.Count == 0)
            throw new ArgumentException("At least one mapping is required.", nameof(mappings));

        foreach (var mapping in mappings)
        {
            ArgumentNullException.ThrowIfNull(mapping);
            mapping.Validate();
        }

        var existingMappings = await ReadMappingsAsync(cancellationToken).ConfigureAwait(false);
        var allMappings = TickerMappingPeriodValidator.RemoveExactDuplicates(
            existingMappings.Concat(mappings));
        TickerMappingPeriodValidator.EnsureNoOverlaps(allMappings);

        if (allMappings.Count == existingMappings.Count)
            return;

        var orderedMappings = allMappings
            .OrderBy(mapping => mapping.Ticker, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.Provider, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.StartDate)
            .ToArray();
        await WriteMappingsAtomicallyAsync(orderedMappings, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TickerProviderMapping>> GetMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        var mappings = await ReadMappingsAsync(cancellationToken).ConfigureAwait(false);
        return SortMappings(mappings);
    }

    public async Task<IReadOnlyList<TickerProviderMapping>> GetMappingsForPeriodAsync(
        string ticker,
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var mappings = await ReadMappingsAsync(cancellationToken).ConfigureAwait(false);
        return SortMappings(mappings.Where(mapping =>
            string.Equals(mapping.Ticker, ticker, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(mapping.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
            DateOnly.FromDateTime(mapping.StartDate) <= to &&
            DateOnly.FromDateTime(mapping.EndDate) >= from));
    }

    #endregion

    #region JSON file access

    private async Task<IReadOnlyList<TickerProviderMapping>> ReadMappingsAsync(
        CancellationToken cancellationToken)
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(_dataFilePath, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }

        try
        {
            var mappings = json.Déserialize<List<TickerProviderMapping>>()
                ?? throw new InvalidDataException(
                    $"The referential JSON database '{_dataFilePath}' must contain a JSON array, not null.");
            foreach (var mapping in mappings)
                mapping.Validate();

            TickerMappingPeriodValidator.EnsureNoOverlaps(mappings);
            return mappings;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"The referential JSON database '{_dataFilePath}' is invalid. Expected a JSON array of mappings.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                $"The referential JSON database '{_dataFilePath}' contains an invalid mapping.",
                exception);
        }
        catch (TickerReferenceConflictException exception)
        {
            throw new InvalidDataException(
                $"The referential JSON database '{_dataFilePath}' contains conflicting mapping periods.",
                exception);
        }
    }

    private async Task WriteMappingsAtomicallyAsync(
        IReadOnlyCollection<TickerProviderMapping> mappings,
        CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_dataFilePath)
            ?? throw new InvalidOperationException($"The data file path '{_dataFilePath}' has no parent directory.");
        Directory.CreateDirectory(directoryPath);

        var temporaryFilePath = Path.Combine(
            directoryPath,
            $"{Path.GetFileName(_dataFilePath)}.{Guid.NewGuid():N}.tmp");
        var json = mappings.Serialize();

        try
        {
            await File.WriteAllTextAsync(temporaryFilePath, json, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryFilePath, _dataFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
                File.Delete(temporaryFilePath);
        }
    }

    private static IReadOnlyList<TickerProviderMapping> SortMappings(
        IEnumerable<TickerProviderMapping> mappings) =>
        mappings
            .OrderBy(mapping => mapping.Ticker, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.Provider, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.StartDate)
            .ToArray();

    #endregion
}
