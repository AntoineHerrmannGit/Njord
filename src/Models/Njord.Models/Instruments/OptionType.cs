namespace Njord.Models.Instruments;

/// <summary>Identifies the direction of an option contract.</summary>
public enum OptionType
{
    /// <summary>Right to buy the underlying instrument.</summary>
    Call,

    /// <summary>Right to sell the underlying instrument.</summary>
    Put
}
