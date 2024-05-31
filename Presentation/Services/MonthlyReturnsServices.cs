using AppCommon.NYSECalendar.Compute;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.AppModels;

namespace Presentation.Services;

public class MonthlyReturnsServices(
    ILogger<MonthlyReturnsServices> logger,
    IDbContextFactory<AppDbContext> contextFactory) : IMonthlyReturnsServices
{
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;
    private readonly ILogger<MonthlyReturnsServices> logger = logger;

    public async Task<List<DateTime>> GetComputedDatesAsync()
    {
        using var context = contextFactory.CreateDbContext();
        try
        {
            List<DateTime> distinctDates = await context.SelectedTickers
                .Select(t => t.Date)
                .Distinct()
                .OrderBy(t => t.Date)
                .ToListAsync();
            return distinctDates;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error when trying to get Computed Dates");
        }
        return [];
    }

    public async Task<List<VirtualReturns>> GetPricesForGivenMonthAsync(TickersForDate tickersForDate, double totalFunds)
    {
        List<VirtualReturns> virtualReturns = [];
        DateTime startDate = tickersForDate.Date;
        if (startDate.Date != TradingCalendar.FirstTradingDayOfMonth(startDate.Month, startDate.Year).Date)
        {
            logger.LogDebug($"Given date {startDate} is not first trading day of the month");
            return virtualReturns;
        }
        DateTime endDate = TradingCalendar.FirstTradingDayOfMonth(
            startDate.AddMonths(1).Month, startDate.AddMonths(1).Year)
            .ToUniversalTime();
        logger.LogInformation($"Start Date: {startDate}, End Date: {endDate}");
        try
        {
            string[] tickers = tickersForDate.Tickers.Replace(" ", "").Split(",");

            using var context = contextFactory.CreateDbContext();
            //Had to break my head for half a day to get this working
            //Make sure you know what you are doing before you fiddle with the following LINQ query
            List<PriceByDate> pricesByDate = await context.PriceByDate
                .Where(r => ((r.Date >= startDate.Date && r.Date < startDate.AddDays(1).Date)
                    || (r.Date >= endDate.Date && r.Date < endDate.AddDays(1).Date))
                 && tickers.Contains(r.Ticker))
                .ToListAsync();
            PopulateVirtualReturns(virtualReturns, startDate, endDate, tickers, totalFunds, pricesByDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting data from PriceByDate");
            virtualReturns.Clear();
        }
        return virtualReturns;
    }

    private void PopulateVirtualReturns(List<VirtualReturns> virtualReturns,
        DateTime startDate, DateTime endDate, string[] tickers
        , double totalFunds, List<PriceByDate> pricesByDate)
    {
        double fundsAllocatedPerTicker = totalFunds / tickers.Length;
        foreach (var ticker in tickers)
        {
            List<PriceByDate> requiredRecordsForTicker = pricesByDate
                .Where(r => r.Ticker == ticker)
                .OrderBy(r => r.Date)
                .ToList();

            if (requiredRecordsForTicker.Count == 2)
            {
                double quantity = fundsAllocatedPerTicker / requiredRecordsForTicker[0].Close;
                VirtualReturns virtualReturn = new();
                virtualReturn.SetValues(requiredRecordsForTicker[0], requiredRecordsForTicker[1], quantity);
                virtualReturns.Add(virtualReturn);
            }
            else
            {
                logger.LogError($"{ticker} => Does not have two entries for dates {startDate} and {endDate}");
            }
        }
    }

    public async Task<List<TickersForDate>> GetTickersForDatesAsync()
    {
        using var context = contextFactory.CreateDbContext();
        List<DateTime> distinctDates = await GetComputedDatesAsync();
        if (distinctDates.Count == 0)
        {
            return [];
        }
        List<TickersForDate> tickersForDates = [];
        try
        {
            foreach (var date in distinctDates)
            {
                var concatenatedTickers = await context.SelectedTickers
                    .Where(t => t.Date == date)
                    .OrderBy(t => t.Ticker)
                    .Select(t => t.Ticker)
                    .Take(10)
                    .ToListAsync();
                string result = string.Join(", ", concatenatedTickers);
                tickersForDates.Add(new() { Date = date, Tickers = result });
            }
            tickersForDates = [.. tickersForDates.OrderBy(t => t.Date)];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error when trying to get Selected Tickers by Date");
        }
        return tickersForDates;
    }
}