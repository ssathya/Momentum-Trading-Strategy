using ComputeMomentum.Internal;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeMomentum.Services;

internal class Computation(ILogger<Computation> logger, MomentumDbMethods momentumDbMethods)
{
    private readonly ILogger<Computation> logger = logger;
    private readonly MomentumDbMethods momentumDbMethods = momentumDbMethods;

    public void ComputeSelectedTickersForPeriod(List<DataFrameSim> results, List<SelectedTicker> selectedTickers)
    {
        List<TickerGainByDate> yearlyPercentGains = [];
        yearlyPercentGains = ComputeYearlyTopers(results, yearlyPercentGains);
        List<TickerGainByDate> halfYearPercentGains = [];
        halfYearPercentGains = ComputeHalfYearlyTopers(results, yearlyPercentGains, halfYearPercentGains);
        List<TickerGainByDate> quarterYearPercentGains = [];
        quarterYearPercentGains = ComputeQuarterYearlyTopers(results, halfYearPercentGains, quarterYearPercentGains);

        foreach (var selectedTicker in quarterYearPercentGains)
        {
            if (results.Find(r => r.Ticker.Equals(selectedTicker.Ticker)) == null)
            {
                continue;
            }
            if (results.Find(r => r.Ticker.Equals(selectedTicker.Ticker)) is not null)
            {
                selectedTickers.Add(new SelectedTicker()
                {
                    Ticker = selectedTicker.Ticker,
                    Date = selectedTicker.ReportedDate.ToDateTime(TimeOnly.Parse("04:00 AM")).ToUniversalTime(),
                    Close = results.Find(r => r.Ticker.Equals(selectedTicker.Ticker))?.ValueByDate.Last().Value ?? 0,
                    AnnualPercentGain = yearlyPercentGains
                    .Find(r => r.Ticker.Equals(selectedTicker.Ticker))?.ChangePercent ?? 0,
                    HalfYearlyPercentGain = halfYearPercentGains
                    .Find(r => r.Ticker.Equals(selectedTicker.Ticker))?.ChangePercent ?? 0,
                    QuarterYearlyPercentGain = selectedTicker.ChangePercent
                });
            }
        }
    }

    public async Task<List<DataFrameSim>> ObtainTickersAndClosingPricesAsync()
    {
        List<string> tickers = await momentumDbMethods.GetAllTickersAsync();

        List<DataFrameSim> results = await momentumDbMethods.GetPricesForTickersAsync(tickers);
        return results;
    }

    private List<TickerGainByDate> ComputeHalfYearlyTopers(List<DataFrameSim> results, List<TickerGainByDate> yearlyPercentGains, List<TickerGainByDate> halfYearPercentGains)
    {
        var dfToConsider = results.Where(r => yearlyPercentGains.Select(x => x.Ticker).Contains(r.Ticker))
            .ToList();
        foreach (var result in dfToConsider)
        {
            TickerGainByDate percentChangeOnDate = ComputeRollingReturn(PeriodDefinition.HalfYearly, result);
            halfYearPercentGains.Add(percentChangeOnDate);
        }
        halfYearPercentGains = halfYearPercentGains.OrderByDescending(x => x.ChangePercent)
            .Take(30)
            .ToList();
        return halfYearPercentGains;
    }

    private List<TickerGainByDate> ComputeQuarterYearlyTopers(List<DataFrameSim> results, List<TickerGainByDate> halfYearPercentGains, List<TickerGainByDate> quarterYearPercentGains)
    {
        var dfToConsider = results.Where(r => halfYearPercentGains.Select(x => x.Ticker).Contains(r.Ticker))
            .ToList();
        foreach (var result in results)
        {
            var check = halfYearPercentGains.FirstOrDefault(x => x.Ticker.Equals(result.Ticker));
            if (check is null)
            { continue; }
            TickerGainByDate percentChangeOnDate = ComputeRollingReturn(PeriodDefinition.QuarterYearly, result);
            quarterYearPercentGains.Add(percentChangeOnDate);
        }
        quarterYearPercentGains = quarterYearPercentGains.OrderByDescending(x => x.ChangePercent)
            .Take(10)
            .ToList();
        return quarterYearPercentGains;
    }

    private TickerGainByDate ComputeRollingReturn(PeriodDefinition pd, DataFrameSim data)
    {
        double firstDateValue = data.ValueByDate.Last().Value;
        DateOnly firstDate = data.ValueByDate.Last().Key;
        string logMsgHolder = string.Empty;
        DateOnly secondDate;
        switch (pd)
        {
            case PeriodDefinition.Annual:
                secondDate = firstDate.AddYears(-1);
                logMsgHolder = "year";
                break;

            case PeriodDefinition.HalfYearly:
                secondDate = firstDate.AddMonths(-6);
                logMsgHolder = "half year";
                break;

            case PeriodDefinition.QuarterYearly:
                secondDate = firstDate.AddMonths(-3);
                logMsgHolder = "quarter";
                break;

            default:
                return new TickerGainByDate { Ticker = data.Ticker };
        }
        secondDate = data.ValueByDate.Keys.LastOrDefault(r => r <= secondDate);
        if (secondDate == new DateOnly(1, 1, 1))
        {
            return new TickerGainByDate { Ticker = data.Ticker };
        }
        double percentGainLoss = ((firstDateValue / data.ValueByDate[secondDate]) - 1) * 100.0;
        if (percentGainLoss < 0)
        {
            logger?.LogInformation($"{data.Ticker} has gone down for the {logMsgHolder} {percentGainLoss.ToString("F2")}% between {secondDate.ToShortDateString()} and {firstDate.ToShortDateString()}");
        }

        return new TickerGainByDate
        {
            Ticker = data.Ticker,
            ReportedDate = firstDate,
            ChangePercent = percentGainLoss
        };
    }

    private List<TickerGainByDate> ComputeYearlyTopers(List<DataFrameSim> results, List<TickerGainByDate> yearlyPercentGains)
    {
        foreach (var result in results)
        {
            TickerGainByDate percentChangeOnDate = ComputeRollingReturn(PeriodDefinition.Annual, result);
            yearlyPercentGains.Add(percentChangeOnDate);
        }
        yearlyPercentGains = yearlyPercentGains.OrderByDescending(x => x.ChangePercent)
            .Take(50)
            .ToList();
        return yearlyPercentGains;
    }
}

internal enum PeriodDefinition
{
    Annual = 1,
    HalfYearly = 2,
    QuarterYearly = 3,
    Invalid = 0
}