namespace Njord.Data.Referential.Models;

/// <summary>Describes an instrument's Njord asset class independently from a data provider.</summary>
public enum NjordAssetClass
{
    Stock,
    Future,
    Option,
    Index,
    Forex,
    Crypto,
    YieldCurve,
    Rate,
    Swap,
    Bond,
    Repo
}
