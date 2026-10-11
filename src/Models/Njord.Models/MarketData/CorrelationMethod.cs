namespace Njord.Models.MarketData;

/// <summary>Identifies the statistical method used to calculate a correlation.</summary>
public enum CorrelationMethod
{
    /// <summary>Linear correlation measured using covariance and standard deviations.</summary>
    Pearson,

    /// <summary>Rank correlation measured using ranked observations.</summary>
    Spearman
}
