using AppCommon.NYSECalendar.Compute;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeMomentum.Services;

internal class MaintainComputeValues(ILogger<MaintainComputeValues> logger, IDbContextFactory<AppDbContext> contextFactory)
{
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;
    private readonly ILogger<MaintainComputeValues> logger = logger;

    public async Task<bool> DeleteAgedRecords()
    {
        DateTime referenceDate = DateTime.Now.AddMonths(-1);
        List<DateTime> MonthStartDates = [];

        for (int i = 0; i < 12; i++)
        {
            MonthStartDates.Add(TradingCalendar.FirstTradingDayOfMonth(referenceDate.Month, referenceDate.Year)
                .ToUniversalTime());
            referenceDate = referenceDate.AddMonths(-1);
        }
        referenceDate = DateTime.Now.AddMonths(-1);
        DateTime lastTradingDatePrevMonth = TradingCalendar.LastTradingDayOfMonth(referenceDate.Month, referenceDate.Year)
            .ToUniversalTime();
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            await context.SelectedTickers.Where(x => x.LastUpdated <= lastTradingDatePrevMonth &&
                !MonthStartDates.Contains(x.LastUpdated))
                .ExecuteDeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical("Unable to delete records from SelectedTickers");
            logger.LogCritical(ex.Message);
            return false;
        }
    }

    private async Task<List<IndexComponent>> GetAllIndexComponentsAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            List<IndexComponent> indexComponents = await context.IndexComponents.ToListAsync();
            return indexComponents;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reading Index Components");
            return [];
        }
    }

    public async Task<bool> UpdateSelectedPositions(List<SelectedTicker>? selectedPositions)
    {
        if (selectedPositions == null || selectedPositions.Count == 0)
        {
            return false;
        }
        List<IndexComponent> indexComponents = await GetAllIndexComponentsAsync();
        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            List<DateTime> recordsToUpdate = selectedPositions.Select(x => x.Date).Distinct().ToList();
            List<SelectedTicker> spInDb = await context.SelectedTickers
                .Where(x => recordsToUpdate.Contains(x.Date))
                .AsNoTracking()
                .ToListAsync();
            foreach (var selectedTicker in selectedPositions)
            {
                var indexComponent = indexComponents.FirstOrDefault(r => r.Ticker == selectedTicker.Ticker);
                if (indexComponent != null)
                {
                    selectedTicker.CompanyName = indexComponent.CompanyName;
                }
                SelectedTicker? existingRecord = spInDb.FirstOrDefault(r => r.Ticker == selectedTicker.Ticker
                && r.Date == selectedTicker.Date);
                if (existingRecord == null)
                {
                    spInDb.Add(selectedTicker);
                }
                else
                {
                    existingRecord.Close = selectedTicker.Close;
                    existingRecord.Date = selectedTicker.Date;
                    existingRecord.AnnualPercentGain = selectedTicker.AnnualPercentGain;
                    existingRecord.HalfYearlyPercentGain = selectedTicker.HalfYearlyPercentGain;
                    existingRecord.QuarterYearlyPercentGain = selectedTicker.QuarterYearlyPercentGain;
                    existingRecord.LastUpdated = selectedTicker.LastUpdated;
                    existingRecord.CompanyName = selectedTicker.CompanyName;
                }
            }
            List<SelectedTicker> spInDb1 = spInDb.Where(r => r.Id != 0)
                .ToList();
            await context.BulkUpdateAsync(spInDb1).ConfigureAwait(false);
            spInDb1 = spInDb.Where(r => r.Id == 0)
                .ToList();
            await context.BulkInsertAsync(spInDb1).ConfigureAwait(false);
            //await context.BulkInsertOrUpdateAsync(spInDb);
            await context.BulkSaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical("Unable to update database");
            logger.LogCritical(ex.Message);
            return false;
        }
    }
}