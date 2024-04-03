using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.Extensions;

namespace SecuritiesMaintain.Services;

internal class IndexToDbService(ILogger<IndexToDbService> logger, IDbContextFactory<AppDbContext> contextFactory) : IIndexToDbService
{
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;
    private readonly ILogger<IndexToDbService> logger = logger;

    public async Task<bool> DeleteAgedRecords()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            await context.IndexComponents.Where(x => x.LastUpdated <= DateTime.UtcNow.AddYears(-1))
                .ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical("Unable to delete records");
            logger.LogCritical(ex.Message);
            return false;
        }
        return true;
    }

    public async Task<int> SelectCurrentIndexCountAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            var icInDbCount = await context.IndexComponents.CountAsync();
            return icInDbCount;
        }
        catch (Exception)
        {
            logger.LogWarning("Database should have timed out");
            return 0;
        }
    }

    public async Task<bool> UpdateIndexList(List<IndexComponent>? indexComponents)
    {
        if (indexComponents == null || indexComponents.Count == 0)
        {
            return false;
        }
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            var icInDb = await context.IndexComponents.AsNoTracking().ToListAsync();
            foreach (IndexComponent component1 in icInDb)
            {
                component1.DowWeight = 0;
                component1.NasdaqWeight = 0;
                component1.SnPWeight = 0;
            }

            foreach (var (component, existingRecord) in from IndexComponent component in indexComponents
                                                        let existingRecord = icInDb.FirstOrDefault(x => x.Ticker == component.Ticker)
                                                        select (component, existingRecord))
            {
                if (existingRecord == null)
                {
                    icInDb.Add(component);
                }
                else
                {
                    existingRecord.SetNewValues(component);
                }
            }

            await context.BulkInsertOrUpdateAsync(icInDb);
        }
        catch (Exception ex)
        {
            logger.LogCritical("Unable to update database");
            logger.LogCritical(ex.Message);
            return false;
        }
        return true;
    }
}