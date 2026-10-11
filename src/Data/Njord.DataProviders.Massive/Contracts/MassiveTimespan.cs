namespace Njord.DataProviders.Massive.Contracts;

/// <summary>Identifies a historical aggregate bar interval supported by Massive.</summary>
public enum MassiveTimespan
{
    Second,
    Minute,
    Hour,
    Day,
    Session,
    Week,
    Month,
    Quarter,
    Year
}
