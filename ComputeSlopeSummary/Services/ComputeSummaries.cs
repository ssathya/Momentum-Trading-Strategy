using Microsoft.Extensions.Logging;
using Models;

namespace ComputeSlopeSummary.Services;

internal class ComputeSummaries(ILogger<ComputeSummaries> logger, SummaryDbMethods dbMethods)
{
    private readonly ILogger<ComputeSummaries> logger = logger;
    private readonly SummaryDbMethods dbMethods = dbMethods;

    public async Task<bool> GenerateSummaries()
    {
        await dbMethods.TruncateTableSlopeSummaries();
        List<SlopeSummary> slopeSummaries = [];
        foreach (Period period in Enum.GetValues(typeof(Period)))
        {
            List<TickerSlope> tickerSlopes = await dbMethods.GetTickerSlopes(period);
            foreach (TickerSlope tickerSlope in tickerSlopes)
            {
                DateTime endDate = tickerSlope.SlopeResults.Last().Date;
                DateTime startDate = ComputeStartDate(period, tickerSlope, endDate);
                double PeriodStartSlope = tickerSlope.SlopeResults.Where(r => r.Date == startDate).First().Slope ?? 0;
                double PeriodEndSlope = tickerSlope.SlopeResults.Where(r => r.Date == endDate).First().Slope ?? 0;
                if (PeriodStartSlope == 0)
                {
                    PeriodStartSlope = 0.000001;
                }
                double percentChange = ((PeriodEndSlope - PeriodStartSlope) / Math.Abs(PeriodStartSlope)) * 100;
                slopeSummaries.Add(new SlopeSummary
                {
                    Ticker = tickerSlope.Ticker,
                    Period = period,
                    PeriodStart = tickerSlope.SlopeResults.Where(r => r.Date == startDate).First(),
                    PeriodEnd = tickerSlope.SlopeResults.Where(r => r.Date == endDate).First(),
                    SlopeChangePercentage = (float)percentChange
                });
            }
        }
        bool storeResult = await dbMethods.StoreSlopeSummary(slopeSummaries);
        return storeResult;
    }

    private static DateTime ComputeStartDate(Period period, TickerSlope tickerSlope, DateTime endDate)
    {
        DateTime startDate;
        switch (period)
        {
            case Period.Yearly:
                var dateToConsider = endDate.AddYears(-1);
                var recordToUse = tickerSlope.SlopeResults.Where(x => x.Date >= dateToConsider).FirstOrDefault();
                startDate = recordToUse != null ? recordToUse.Date : endDate;
                break;

            case Period.HalfYearly:
                dateToConsider = endDate.AddMonths(-6);
                recordToUse = tickerSlope.SlopeResults.Where(x => x.Date >= dateToConsider).FirstOrDefault();
                startDate = recordToUse != null ? recordToUse.Date : endDate;
                break;

            case Period.Quarterly:
                dateToConsider = endDate.AddMonths(-3);
                recordToUse = tickerSlope.SlopeResults.Where(x => x.Date >= dateToConsider).FirstOrDefault();
                startDate = recordToUse != null ? recordToUse.Date : endDate;
                break;

            case Period.Monthly:
                dateToConsider = endDate.AddMonths(-1);
                recordToUse = tickerSlope.SlopeResults.Where(x => x.Date >= dateToConsider).FirstOrDefault();
                startDate = recordToUse != null ? recordToUse.Date : endDate;
                break;

            default:
                startDate = endDate;
                break;
        }

        return startDate;
    }
}