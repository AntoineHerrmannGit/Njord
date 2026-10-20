namespace Njord.Data.Referential.Configuration;

/// <summary>Local JSON database settings owned by the instrument referential.</summary>
public sealed class ReferentialOptions
{
    #region Referential configuration

    /// <summary>Gets the configuration section owned by the referential.</summary>
    public const string SectionName = "Referential";

    /// <summary>Gets or sets the path of the primary JSON database file.</summary>
    public string DataFilePath { get; set; } = "data/referential/instruments.json";

    #endregion
}
