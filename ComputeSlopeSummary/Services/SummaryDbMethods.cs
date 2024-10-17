using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeSlopeSummary.Services;

internal class SummaryDbMethods(ILogger<SummaryDbMethods> logger, IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly ILogger<SummaryDbMethods> logger = logger;
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;
    private const int batchSize = 100;

    public async Task<List<TickerSlope>> GetTickerSlopes(Period period)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        List<TickerSlope> tickerSlopes = await context.TickerSlopes.Where(p => p.Period == period).ToListAsync();
        return tickerSlopes;
    }

    public async Task<List<SlopeSummary>> GetSlopeSummaryAsync(Period period)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        List<SlopeSummary> slopeSummaries = await context.SlopeSummaries.Where(p => p.Period == period).ToListAsync();
        return slopeSummaries;
    }

    public async Task<Dictionary<string, string>> GetCompanyNamesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        Dictionary<string, string> companyNames = [];
        foreach (var item in await context.IndexComponents.Select(x => new { x.Ticker, x.CompanyName }).ToListAsync())
        {
            companyNames[item.Ticker] = item.CompanyName ?? "";
        }
        return companyNames;
    }

    public async Task<bool> StoreSlopeSummary(List<SlopeSummary> slopeSummaries)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            using var trans = context.Database.BeginTransaction();
            for (int i = 0; i < slopeSummaries.Count; i += batchSize)
            {
                await context.SlopeSummaries.AddRangeAsync(slopeSummaries.Skip(i).Take(batchSize));
                await context.SaveChangesAsync();
            }
            trans.Commit();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occured while storing slope summaries");
            logger.LogCritical($"Error: {ex}");
            return false;
        }
    }

    public async Task TruncateTableSlopeSummaries()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            using var trans = context.Database.BeginTransaction();
            await context.TruncateAsync<SlopeSummary>();
            await context.BulkSaveChangesAsync();
            trans.Commit();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occured while truncating table SlopeSummaries");
            logger.LogCritical($"Error: {ex}");
            return;
        }
    }
}