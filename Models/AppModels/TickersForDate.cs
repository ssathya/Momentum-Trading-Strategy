namespace Models.AppModels;

public class TickersForDate
{
    public DateTime Date { get; set; }
    public string Tickers { get; set; } = string.Empty;
}