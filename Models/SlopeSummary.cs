namespace Models;

public class SlopeSummary
{
    public Guid Id { get; set; }
    public Period Period { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public ComputedSlope PeriodStart { get; set; } = new();
    public ComputedSlope PeriodEnd { get; set; } = new();
    public float SlopeChangePercentage { get; set; }
}