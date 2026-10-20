namespace Njord.DataProviders.Massive.Contracts;

/// <summary>Identifies the Massive market-data endpoint family for a ticker.</summary>
internal enum MassiveAssetClass
{
    Stock,
    Index,
    Option,
    Future,
    Crypto,
    Forex
}
