using ComputeMomentum.Internal;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeMomentum.Services;

internal class MomentumDbMethods(ILogger<MomentumDbMethods> logger, IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly ILogger<MomentumDbMethods> logger = logger;
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;
    private const int batchSize = 100;

    public async Task<List<string>> GetAllTickersAsync(IndexNames indexNames = IndexNames.None)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        List<string> tickers = [];
        if (indexNames == IndexNames.None)
        {
            indexNames = IndexNames.SnP | IndexNames.Nasdaq | IndexNames.Dow;
        }
        try
        {
            tickers = (await context.IndexComponents
               .Where(r => r.LastUpdated >= DateTime.UtcNow.Date &&
                    (r.ListedIndexes & indexNames) != IndexNames.None)
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

    public async Task<List<DataFrameSim>> GetPricesForTickersAsync(List<string> tickers)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        List<DataFrameSim> results = [];
        int incrementCount = 100;
        try
        {
            for (int i = 0; i < tickers.Count; i += incrementCount)
            {
                var selectedTickers = tickers.Skip(i).Take(incrementCount).ToList();
                var dbObjects = await context.PriceByDate
                    .Select(r => new { r.Date, r.Ticker, r.Close })
                    .Where(r => selectedTickers.Contains(r.Ticker)).ToListAsync();
                foreach (var dbObject in dbObjects)
                {
                    var resultObj = results.Where(r => r.Ticker == dbObject.Ticker).FirstOrDefault();
                    if (resultObj is null)
                    {
                        resultObj = new DataFrameSim
                        {
                            Ticker = dbObject.Ticker,
                        };
                        results.Add(resultObj);
                    }
                    resultObj.ValueByDate.Add(DateOnly.FromDateTime(dbObject.Date), (double)dbObject.Close);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error occurred while trying to extract prices from database");
            logger.LogError(ex.Message);
        }
        return results;
    }

    public async Task<List<PriceByDate>> GetOHLCVForAllTickersAsync(List<string> tickers)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        var results = new List<PriceByDate>();
        try
        {
            await context.BulkReadAsync(results);
            results = await context.PriceByDate.Where(r => tickers.Contains(r.Ticker)).ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("Error occurred while trying to extract prices from database");
            logger.LogError(ex.Message);
        }
        return results;
    }

    public async Task<bool> StoreSlopes(List<TickerSlope> tickerSlopes, bool truncateTable = false)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            if (truncateTable)
            {
                using var trans = context.Database.BeginTransaction();
                await context.TruncateAsync<TickerSlope>();
                await context.BulkSaveChangesAsync();
                trans.Commit();
            }
            using var transaction = context.Database.BeginTransaction();
            for (int i = 0; i < tickerSlopes.Count; i += batchSize)
            {
                await context.TickerSlopes.AddRangeAsync(tickerSlopes.Skip(i).Take(batchSize));
                await context.SaveChangesAsync();
            }
            transaction.Commit();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occurred while trying to update Ticker Slopes");
            logger.LogCritical(ex.Message);
            return false;
        }
        return true;
    }
}