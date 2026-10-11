namespace Njord.MarketData;

/// <summary>Identifies whether an option contract is a call or a put.</summary>
public enum OptionType
{
    /// <summary>Right to buy the underlying instrument.</summary>
    Call,

    /// <summary>Right to sell the underlying instrument.</summary>
    Put
}
