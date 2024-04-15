using Microsoft.EntityFrameworkCore;
using OoplesFinance.YahooFinanceAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace Models;

[Index(nameof(Ticker), IsUnique = false)]
public class PriceByDate
{
    [Required]
    public int Id { get; set; }

    [MaxLength(8), Required]
    public string Ticker { get; set; } = string.Empty;

    public DateTime Date { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double AdjClose { get; set; }
    public double Volume { get; set; }

    public static PriceByDate GeneratePriceByDate(HistoricalData historicalData, string ticker)
    {
        return new PriceByDate
        {
            Ticker = ticker,
            Date = historicalData.Date,
            Open = historicalData.Open,
            High = historicalData.High,
            Low = historicalData.Low,
            Close = historicalData.Close,
            AdjClose = historicalData.AdjClose,
            Volume = historicalData.Volume
        };
    }
}