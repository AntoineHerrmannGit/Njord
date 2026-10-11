namespace Njord.DataProviders.Massive.Contracts;

/// <summary>Describes a historical OHLC aggregate request using Massive ticker symbology.</summary>
public sealed record MassiveHistoricalOhlcRequest
{
    #region Request identity

    private string _ticker = string.Empty;

    /// <summary>Gets the Massive ticker, such as AAPL, I:SPX, C:EURUSD, X:BTCUSD, or O:...</summary>
    public required string Ticker
    {
        get => _ticker;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _ticker = value;
        }
    }

    /// <summary>Gets the asset class selecting the Massive endpoint.</summary>
    public required MassiveAssetClass AssetClass { get; init; }

    #endregion

    #region Historical range

    /// <summary>Gets the first requested date.</summary>
    public required DateOnly From { get; init; }

    /// <summary>Gets the last requested date.</summary>
    public required DateOnly To { get; init; }

    /// <summary>Gets the number of time units in each aggregate bar.</summary>
    public required int Multiplier { get; init; }

    /// <summary>Gets the time unit used to construct each aggregate bar.</summary>
    public required MassiveTimespan Timespan { get; init; }

    /// <summary>Gets whether split-adjusted data should be requested when supported.</summary>
    public bool? Adjusted { get; init; }

    #endregion

    #region Validation

    /// <summary>Validates request values and asset-specific interval constraints.</summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Ticker);

        if (!Enum.IsDefined(AssetClass))
            throw new ArgumentOutOfRangeException(nameof(AssetClass), AssetClass, "The asset class is not supported.");

        if (!Enum.IsDefined(Timespan))
            throw new ArgumentOutOfRangeException(nameof(Timespan), Timespan, "The timespan is not supported.");

        if (From > To)
            throw new ArgumentException("The start date must not be after the end date.");

        if (Multiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(Multiplier), Multiplier, "The multiplier must be positive.");

        if (Timespan == MassiveTimespan.Session && AssetClass != MassiveAssetClass.Future)
            throw new ArgumentException("Session aggregates are only supported for futures.", nameof(Timespan));

        if (Timespan == MassiveTimespan.Day && AssetClass == MassiveAssetClass.Future)
            throw new ArgumentException("Daily futures aggregates use the session timespan.", nameof(Timespan));

        if (AssetClass == MassiveAssetClass.Future && Adjusted.HasValue)
            throw new ArgumentException("The Massive futures aggregates endpoint does not support split adjustment.", nameof(Adjusted));
    }

    #endregion
}
