using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

namespace SecurityPriceMaintain.Services;

internal class SecuritiesPriceDbInterface(ILogger<SecuritiesPriceDbInterface> logger,
    IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly ILogger<SecuritiesPriceDbInterface> logger = logger;
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;

    public async Task<List<string>> GetAllTickersAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        List<string> tickers = [];

        try
        {
            tickers = (await context.IndexComponents
               .Where(r => r.LastUpdated >= DateTime.UtcNow.Date)
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
            .Distinct();
        foreach (var ticker in tickersToConsider)
        {
            if (!string.IsNullOrEmpty(ticker))
            {
                logger.LogInformation($"Processing {ticker}");
                await DeleteRecordsForTickerAsync(ticker);
            }
        }
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

    public async Task DeleteRecordsForTickerAsync(string ticker)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            await context.PriceByDate.Where(x => ticker.Equals(x.Ticker))
                .ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error deleting aged records");
            logger.LogCritical(ex.Message);
        }
    }
}