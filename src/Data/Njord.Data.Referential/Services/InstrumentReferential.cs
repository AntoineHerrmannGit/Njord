using Njord.Data.Referential.Contracts;
using Njord.Data.Referential.Errors;
using Njord.Data.Referential.Models;
using Njord.Data.Referential.Persistence;
using Njord.Data.Referential.Validation;

namespace Njord.Data.Referential.Services;

internal sealed class InstrumentReferential : IInstrumentReferential
{
    #region Dependencies

    private readonly IInstrumentMappingRepository _repository;

    public InstrumentReferential(IInstrumentMappingRepository repository)
    {
        _repository = repository;
    }

    #endregion

    #region Mapping management

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

        var distinctMappings = TickerMappingPeriodValidator.RemoveExactDuplicates(mappings);
        TickerMappingPeriodValidator.EnsureNoOverlaps(distinctMappings);
        await _repository.AddMappingsAsync(distinctMappings, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<TickerProviderMapping>> GetMappingsAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetMappingsAsync(cancellationToken);

    #endregion

    #region Mapping resolution

    public async Task<IReadOnlyList<TickerProviderMapping>> ResolveAsync(
        string ticker,
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticker);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (from > to)
            throw new ArgumentException("The start date must not be after the end date.");

        var mappings = await _repository
            .GetMappingsForPeriodAsync(
                ticker,
                provider,
                from,
                to,
                cancellationToken)
            .ConfigureAwait(false);

        if (mappings.Count == 0)
            throw new TickerReferenceResolutionException(
                $"No mapping exists for Njord ticker '{ticker}', provider '{provider}', and period {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");

        TickerMappingPeriodValidator.EnsureNoOverlaps(mappings);
        TickerMappingPeriodValidator.EnsureCompleteCoverage(ticker, provider, from, to, mappings);
        return mappings;
    }

    #endregion
}
