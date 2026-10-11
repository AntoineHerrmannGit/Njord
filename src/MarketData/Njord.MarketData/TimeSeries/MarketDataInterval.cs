namespace Njord.MarketData;

/// <summary>Time interval represented by each historical market-data bar.</summary>
public enum MarketDataInterval
{
    /// <summary>One-minute bars.</summary>
    OneMinute,

    /// <summary>Two-minute bars.</summary>
    TwoMinutes,

    /// <summary>Five-minute bars.</summary>
    FiveMinutes,

    /// <summary>Fifteen-minute bars.</summary>
    FifteenMinutes,

    /// <summary>Thirty-minute bars.</summary>
    ThirtyMinutes,

    /// <summary>Sixty-minute bars.</summary>
    SixtyMinutes,

    /// <summary>Ninety-minute bars.</summary>
    NinetyMinutes,

    /// <summary>One-hour bars.</summary>
    OneHour,

    /// <summary>One-day bars.</summary>
    OneDay,

    /// <summary>Five-day bars.</summary>
    FiveDays,

    /// <summary>One-week bars.</summary>
    OneWeek,

    /// <summary>One-month bars.</summary>
    OneMonth,

    /// <summary>Three-month bars.</summary>
    ThreeMonths
}
