using Njord.DataProviders.Massive.Contracts;
using Njord.DataProviders.Massive.Serialization;

namespace Njord.DataProviders.Massive.Mapping;

internal sealed record MassiveOhlcMappingInput
{
    #region Mapping input

    public required IReadOnlyList<MassiveAggregateResponseDto> Pages { get; init; }

    public required MassiveResolvedOhlcRequest Request { get; init; }

    #endregion
}
