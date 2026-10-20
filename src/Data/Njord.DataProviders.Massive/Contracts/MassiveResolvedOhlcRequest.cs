namespace Njord.DataProviders.Massive.Contracts;

internal sealed record MassiveResolvedOhlcRequest
{
    #region Ticker resolution

    public required string NjordTicker { get; init; }

    public required string Ticker { get; init; }

    public required MassiveAssetClass AssetClass { get; init; }

    #endregion

    #region Historical range

    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    public required int Multiplier { get; init; }

    public required MassiveTimespan Timespan { get; init; }

    public bool? Adjusted { get; init; }

    #endregion

    #region Validation

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(NjordTicker);
        ArgumentException.ThrowIfNullOrWhiteSpace(Ticker);

        if (!Enum.IsDefined(AssetClass))
            throw new ArgumentOutOfRangeException(nameof(AssetClass), AssetClass, "The asset class is not supported.");

        if (Timespan == MassiveTimespan.Session && AssetClass != MassiveAssetClass.Future)
            throw new ArgumentException("Session aggregates are only supported for futures.", nameof(Timespan));

        if (AssetClass == MassiveAssetClass.Future && Adjusted.HasValue)
            throw new ArgumentException("The Massive futures aggregates endpoint does not support split adjustment.", nameof(Adjusted));

        if (From > To)
            throw new ArgumentException("The start date must not be after the end date.");

        if (Multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(Multiplier), Multiplier, "The multiplier must be positive.");
    }

    #endregion
}
