using Microsoft.EntityFrameworkCore;

namespace Models;

[Index(nameof(Ticker), Name = "Ticker_IX")]
public class TickerSlope
{
    public Guid Id { get; set; }
    public Period Period { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public List<ComputedSlope> SlopeResults { get; set; } = [];
}

public enum Period
{
    Yearly,
    HalfYearly,
    Quarterly,
    Monthly
}