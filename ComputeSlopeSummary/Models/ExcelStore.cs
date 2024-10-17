using Models;

namespace ComputeSlopeSummary.Models;

internal class ExcelStore
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Period Period { get; set; }
    public double StartingSlope { get; set; }
    public double EndingSlope { get; set; }

    public static implicit operator ExcelStore(SlopeSummary slopeSummary)
    {
        return new ExcelStore
        {
            Ticker = slopeSummary.Ticker,
            Period = slopeSummary.Period,
            StartingSlope = slopeSummary.PeriodStart.Slope ?? 0,
            EndingSlope = slopeSummary.PeriodEnd.Slope ?? 0
        };
    }
}