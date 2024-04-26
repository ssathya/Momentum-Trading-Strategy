using ComputeMomentum.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeMomentum.Services;

internal class MomentumDbMethods(ILogger<MomentumDbMethods> logger, IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly ILogger<MomentumDbMethods> logger = logger;
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;

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
}