using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

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

    public async Task<bool> UpdateIndexList(List<IndexComponent>? indexComponents)
    {
        if (indexComponents == null || indexComponents.Count == 0)
        {
            return false;
        }
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            await context.BulkMergeAsync(indexComponents, options =>
            {
                options.ColumnPrimaryKeyExpression = x => x.Ticker;
            });
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