using OoplesFinance.YahooFinanceAPI.Models;

namespace Models.AppModels;

public class RTCQResult
{
    #region Public Properties

    public string DisplayName { get; set; } = string.Empty;
    public string QuoteSourceName { get; set; } = string.Empty;
    public double? EpsCurrentYear { get; set; }
    public double? EpsForward { get; set; }
    public string MarketCap { get; set; } = string.Empty;
    public string AverageAnalystRating { get; set; } = string.Empty;
    public string FiftyTwoWeekRange { get; set; } = string.Empty;
    public string RegularMarketDayRange { get; set; } = string.Empty;
    public double? ForwardPE { get; set; }
    public double? TrailingPE { get; set; }
    public double? FiftyDayAverage { get; set; }
    public decimal? TwoHundredDayAverage { get; set; }

    #endregion Public Properties

    #region Public Methods

    public static implicit operator RTCQResult(RealTimeQuoteResult quoteResult)
    {
        return new RTCQResult
        {
            DisplayName = quoteResult.DisplayName,
            QuoteSourceName = quoteResult.QuoteSourceName,
            RegularMarketDayRange = quoteResult.RegularMarketDayRange,
            FiftyTwoWeekRange = quoteResult.FiftyTwoWeekRange,
            FiftyDayAverage = Math.Floor(quoteResult.FiftyDayAverage ?? 0 * 100) / 100,
            TwoHundredDayAverage = (decimal)Math.Floor(quoteResult.TwoHundredDayAverage ?? 0 * 100) / 100,
            TrailingPE = Math.Floor(quoteResult.TrailingPE ?? 0 * 100) / 100,
            ForwardPE = Math.Floor(quoteResult.ForwardPE ?? 0 * 100) / 100,
            EpsCurrentYear = Math.Floor(quoteResult.EpsCurrentYear ?? 0 * 100) / 100,
            EpsForward = Math.Floor(quoteResult.EpsForward ?? 0 * 100) / 100,
            MarketCap = StringHelper.ToKMB((double)(quoteResult.MarketCap ?? 0)),
            AverageAnalystRating = quoteResult.AverageAnalystRating
        };
    }

    #endregion Public Methods
}