using Njord.Data.Referential.Models;

namespace Njord.Data.Referential.Contracts;

/// <summary>Stores ticker mappings and resolves provider symbols for a query period.</summary>
public interface IInstrumentReferential
{
    #region Mapping management

    /// <summary>Adds a set of mappings after validating their quotation-date ranges.</summary>
    Task AddMappingsAsync(
        IReadOnlyCollection<TickerProviderMapping> mappings,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all mappings in stable ticker/provider/date order.</summary>
    Task<IReadOnlyList<TickerProviderMapping>> GetMappingsAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Mapping resolution

    /// <summary>Resolves a canonical ticker to complete, non-overlapping provider mappings for a period.</summary>
    Task<IReadOnlyList<TickerProviderMapping>> ResolveAsync(
        string ticker,
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    #endregion
}
