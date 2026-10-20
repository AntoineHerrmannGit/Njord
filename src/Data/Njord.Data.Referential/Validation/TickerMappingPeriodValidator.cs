using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;

namespace Njord.Data.Referential.Validation;

internal static class TickerMappingPeriodValidator
{
    #region Period validation

    public static IReadOnlyList<TickerProviderMapping> RemoveExactDuplicates(
        IEnumerable<TickerProviderMapping> mappings) =>
        mappings.DistinctBy(mapping => new
        {
            mapping.Ticker,
            mapping.Provider,
            mapping.ProviderTicker,
            mapping.AssetClass,
            StartDate = DateOnly.FromDateTime(mapping.StartDate),
            EndDate = DateOnly.FromDateTime(mapping.EndDate)
        }).ToArray();

    public static void EnsureNoOverlaps(IReadOnlyCollection<TickerProviderMapping> mappings)
    {
        var groups = RemoveExactDuplicates(mappings).GroupBy(mapping => new
        {
            Ticker = mapping.Ticker.Trim().ToUpperInvariant(),
            Provider = mapping.Provider.Trim().ToUpperInvariant()
        });

        foreach (var group in groups)
        {
            var ordered = group
                .OrderBy(mapping => DateOnly.FromDateTime(mapping.StartDate))
                .ToArray();

            if (ordered.Any(mapping =>
                !string.Equals(mapping.Ticker, ordered[0].Ticker, StringComparison.Ordinal) ||
                !string.Equals(mapping.Provider, ordered[0].Provider, StringComparison.Ordinal)))
                throw new TickerReferenceConflictException(
                    $"Ticker and provider casing must be consistent for '{ordered[0].Ticker}' and provider '{ordered[0].Provider}'.");

            var latestEndDate = DateOnly.FromDateTime(ordered[0].EndDate);
            for (var index = 1; index < ordered.Length; index++)
            {
                if (DateOnly.FromDateTime(ordered[index].StartDate) <= latestEndDate)
                    throw new TickerReferenceConflictException(
                        $"Validity periods overlap for Njord ticker '{ordered[index].Ticker}' and provider '{ordered[index].Provider}'.");

                var currentEndDate = DateOnly.FromDateTime(ordered[index].EndDate);
                if (currentEndDate > latestEndDate)
                    latestEndDate = currentEndDate;
            }
        }
    }

    public static void EnsureCompleteCoverage(
        string ticker,
        string provider,
        DateOnly from,
        DateOnly to,
        IReadOnlyList<TickerProviderMapping> mappings)
    {
        var cursor = from;
        foreach (var mapping in mappings.OrderBy(mapping => DateOnly.FromDateTime(mapping.StartDate)))
        {
            var start = DateOnly.FromDateTime(mapping.StartDate);
            var end = DateOnly.FromDateTime(mapping.EndDate);

            if (start > cursor)
                throw MissingCoverage(ticker, provider, cursor, start.AddDays(-1));

            if (end >= to)
                return;

            cursor = end.AddDays(1);
        }

        throw MissingCoverage(ticker, provider, cursor, to);
    }

    private static TickerReferenceResolutionException MissingCoverage(
        string ticker,
        string provider,
        DateOnly from,
        DateOnly to) =>
        new($"No mapping covers Njord ticker '{ticker}' for provider '{provider}' from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");

    #endregion
}
