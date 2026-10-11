using Njord.Models.MarketData;

namespace Njord.Models.Instruments;

/// <summary>Represents a financial instrument identified by a ticker.</summary>
public interface IFinancialInstrument : ITimestampedMarketData
{
    #region Instrument identity

    /// <summary>Gets the ticker identifying this instrument.</summary>
    string Ticker { get; }

    #endregion
}
