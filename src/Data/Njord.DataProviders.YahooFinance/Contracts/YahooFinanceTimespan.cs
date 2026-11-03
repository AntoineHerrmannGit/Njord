namespace Njord.DataProviders.YahooFinance.Contracts;

/// <summary>Identifies a historical aggregate interval that may map to a Yahoo Finance chart interval.</summary>
public enum YahooFinanceTimespan
{
    /// <summary>One or more minutes.</summary>
    Minute,

    /// <summary>One hour.</summary>
    Hour,

    /// <summary>One or five days.</summary>
    Day,

    /// <summary>One week.</summary>
    Week,

    /// <summary>One or three months.</summary>
    Month,

    /// <summary>One quarter.</summary>
    Quarter,

    /// <summary>One year.</summary>
    Year
}
