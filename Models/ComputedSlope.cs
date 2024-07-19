using Skender.Stock.Indicators;

namespace Models;

public class ComputedSlope
{
    public DateTime Date { get; set; }
    public double? Slope { get; set; }
    public double? Intercept { get; set; }
    public double? StdDev { get; set; }
    public double? RSquared { get; set; }
    public decimal? Line { get; set; }

    public static implicit operator ComputedSlope(SlopeResult model)
    {
        return new ComputedSlope
        {
            Date = model.Date,
            Slope = model.Slope,
            Intercept = model.Intercept,
            StdDev = model.StdDev,
            RSquared = model.RSquared,
            Line = model.Line
        };
    }
}