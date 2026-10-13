namespace Njord.Core.Retry;

/// <summary>Defines how retry delays grow between attempts.</summary>
public enum RetryDelayPattern
{
    /// <summary>Doubles the delay after each failed attempt.</summary>
    Exponential,

    /// <summary>Adds one base-delay interval after each failed attempt.</summary>
    Linear
}
