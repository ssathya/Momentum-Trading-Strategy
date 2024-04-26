using AppCommon;
using AppCommon.NYSECalendar.Compute;
using ComputeMomentum.Internal;
using ComputeMomentum.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeMomentum;

internal class FunctionHandler
{
    private const string ApplicationName = "ComputeMomentum";
    private ILogger<FunctionHandler>? logger;

    internal async Task<List<SelectedTicker>?> DoApplicationProcessingAsync()
    {
        Console.WriteLine("Setting up application");
        IServiceCollection services = ServiceHandler.ConfigureServices(ApplicationName);
        AppSpecificSettings(services);
        ServiceHandler.ConnectToDb(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Console.WriteLine("Application setup complete");
        logger = provider.GetService<ILogger<FunctionHandler>>();
        MomentumDbMethods? momentumDbMethods = provider.GetService<MomentumDbMethods>();
        if (momentumDbMethods == null)
        {
            logger?.LogError("Unable to create an instance of MomentumDbMethods");
            return null;
        }

        List<string> tickers = await momentumDbMethods.GetAllTickersAsync();

        List<DataFrameSim> results = await momentumDbMethods.GetPricesForTickersAsync(tickers);
        //Remove firms that were newly formed.
        int meanCount = (int)results.Select(x => x.ValueByDate.Count).ToList().Average();
        results = [.. results.Where(r => r.ValueByDate.Count > meanCount).OrderBy(r => r.Ticker)];

        List<SelectedTicker> selectedTickers = [];
        ComputeSelectedTickersForPeriod(results, selectedTickers);

        //If today is the first trading day of the month then we don't want to get a duplicate entry
        //in selected tickers.
        var firstTradingDay = TradingCalendar.FirstTradingDayOfMonth();
        if (results.First().ValueByDate.Last().Key == DateOnly.FromDateTime(firstTradingDay))
        {
            foreach (var result in results)
            {
                result.ValueByDate.Remove(result.ValueByDate.Last().Key);
            }
        }
        for (int i = 0; i < 11; i++)
        {
            var startingDate = results.First().ValueByDate.Last().Key;
            DateOnly fTD = DateOnly.FromDateTime(TradingCalendar.FirstTradingDayOfMonth(startingDate.Month, startingDate.Year));
            RemoveForwardPrices(results, fTD);
            ComputeSelectedTickersForPeriod(results, selectedTickers);
            //NYSE is not closed for more than 3 days in a stretch.
            fTD = fTD.AddDays(-4);
            RemoveForwardPrices(results, fTD);
        }
        return selectedTickers;
    }

    private static void RemoveForwardPrices(List<DataFrameSim> results, DateOnly fTD)
    {
        foreach (var result in results)
        {
            var keysToRemove = result.ValueByDate.Keys.Where(k => k > fTD);
            foreach (DateOnly keyToRemove in keysToRemove)
            {
                result.ValueByDate.Remove(keyToRemove);
            }
        }
    }

    private void ComputeSelectedTickersForPeriod(List<DataFrameSim> results, List<SelectedTicker> selectedTickers)
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
                    .Find(r => r.Ticker.Equals(selectedTicker.Ticker))?.ChangePercent ?? 0
                });
            }
        }
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

    private void AppSpecificSettings(IServiceCollection services)
    { services.AddScoped<MomentumDbMethods>(); }
}

internal enum PeriodDefinition
{
    Annual = 1,
    HalfYearly = 2,
    QuarterYearly = 3,
    Invalid = 0
}