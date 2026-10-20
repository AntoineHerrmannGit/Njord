using Njord.Data.Referential.Models;

namespace Njord.Data.Referential.Persistence;

internal interface IInstrumentMappingRepository
{
    #region Mapping persistence

    Task AddMappingsAsync(
        IReadOnlyCollection<TickerProviderMapping> mappings,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TickerProviderMapping>> GetMappingsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TickerProviderMapping>> GetMappingsForPeriodAsync(
        string ticker,
        string provider,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    #endregion
}
