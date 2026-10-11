namespace Njord.MarketData;

/// <summary>Identifies the asset class represented by a market instrument.</summary>
public enum MarketAssetClass
{
    /// <summary>Equities, including publicly traded stocks.</summary>
    Equity,

    /// <summary>Market indices.</summary>
    Index,

    /// <summary>Options.</summary>
    Option,

    /// <summary>Futures contracts.</summary>
    Future,

    /// <summary>Swaps.</summary>
    Swap,

    /// <summary>Repurchase agreements.</summary>
    Repo,

    /// <summary>Interest-rate instruments.</summary>
    InterestRate,

    /// <summary>Foreign-exchange instruments.</summary>
    ForeignExchange
}
