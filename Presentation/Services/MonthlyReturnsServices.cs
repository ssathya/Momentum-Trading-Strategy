using Microsoft.EntityFrameworkCore;
using Models;
using Models.AppModels;

namespace Presentation.Services;

public class MonthlyReturnsServices(ILogger<MonthlyReturnsServices> logger, IDbContextFactory<AppDbContext> contextFactory)
: IMonthlyReturnsServices
{
    private readonly ILogger<MonthlyReturnsServices> logger = logger;
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;

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
                                .ToListAsync();
                string result = string.Join(", ", concatenatedTickers);
                tickersForDates.Add(new()
                {
                    Date = date,
                    Tickers = result
                });
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