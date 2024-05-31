namespace Models.AppModels;

public class VirtualReturns
{
    public string Ticker { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public double StartPrice { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double EndPrice { get; set; }
    public double Quantity { get; set; }

    public void SetValues(PriceByDate startPriceByDate, PriceByDate endPriceByDate, double quantity)
    {
        Ticker = startPriceByDate.Ticker;
        PeriodStart = startPriceByDate.Date;
        StartPrice = startPriceByDate.Close;
        PeriodEnd = endPriceByDate.Date;
        EndPrice = endPriceByDate.Close;
        Quantity = quantity;
    }
}