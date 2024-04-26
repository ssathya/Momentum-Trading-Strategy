namespace ComputeMomentum.Internal;

public class TickerGainByDate
{
    public DateOnly ReportedDate { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public double ChangePercent { get; set; }
}