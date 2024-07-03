using Microsoft.EntityFrameworkCore;
using NodaTime;
using OoplesFinance.YahooFinanceAPI.Models;
using System.ComponentModel.DataAnnotations;
using YahooQuotesApi;

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
            Date = historicalData.Date.ToUniversalTime().Date,
            Open = historicalData.Open,
            High = historicalData.High,
            Low = historicalData.Low,
            Close = historicalData.Close,
            AdjClose = historicalData.AdjClose,
            Volume = historicalData.Volume
        };
    }

    public static PriceByDate GeneratePriceByDate(PriceTick priceTick, string ticker)
    {
        return new PriceByDate
        {
            Ticker = ticker,
            Date = priceTick.Date.ToDateTimeUnspecified().ToUniversalTime(),
            Open = priceTick.Open,
            High = priceTick.High,
            Low = priceTick.Low,
            Close = priceTick.Close,
            AdjClose = priceTick.AdjustedClose,
            Volume = priceTick.Volume
        };
    }
}