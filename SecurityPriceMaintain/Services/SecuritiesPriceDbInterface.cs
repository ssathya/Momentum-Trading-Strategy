using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

namespace SecurityPriceMaintain.Services;

internal class SecuritiesPriceDbInterface(ILogger<SecuritiesPriceDbInterface> logger,
    IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;
    private readonly ILogger<SecuritiesPriceDbInterface> logger = logger;

    public async Task DeleteRecordsForTickersAsync(List<string> tickers)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            await context.PriceByDate.Where(x => tickers.Contains(x.Ticker))
                .ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error deleting aged records");
            logger.LogCritical(ex.Message);
        }
    }

    public async Task<bool> DropAgedRecords(List<string> tickers)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            IEnumerable<string> droppedTickers = await context.PriceByDate.Select(x => x.Ticker)
                .Distinct()
                .Except(tickers)
                .ToListAsync();

            if (droppedTickers.Any())
            {
                await context.PriceByDate.Where(x => droppedTickers.Contains(x.Ticker))
                    .ExecuteDeleteAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Exception occurred deleting records that are not in index");
            logger.LogError(ex.Message);
            return false;
        }

        return true;
    }

    public async Task<List<string>> GetAllTickersAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        List<string> tickers = [];

        try
        {
            tickers = (await context.IndexComponents
               .Where(r => r.LastUpdated >= DateTime.UtcNow.Date)
               .AsNoTracking()
               .Select(x => x.Ticker ?? "").ToListAsync());
        }
        catch (Exception ex)
        {
            logger.LogCritical("Unable to get index tickers");
            logger.LogCritical(ex.Message);
        }
        return tickers;
    }

    public async Task<bool> StorePricingValue(List<PriceByDate> prices)
    {
        var tickersToConsider = prices.Select(x => x.Ticker)
            .Distinct()
            .ToList();
        await DeleteRecordsForTickersAsync(tickersToConsider);
        try
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.BulkInsertAsync(prices);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical("Unable to add records to Database");
            logger.LogCritical(ex.Message);
            return false;
        }
    }
}