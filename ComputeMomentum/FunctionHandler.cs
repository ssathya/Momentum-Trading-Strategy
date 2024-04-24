using AppCommon;
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

    internal async Task DoApplicationProcessingAsync()
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
            return;
        }
        //List<string> spTickers = await momentumDbMethods.GetAllTickersAsync(IndexNames.SnP);
        List<string> nasTickers = await momentumDbMethods.GetAllTickersAsync(IndexNames.Nasdaq);
        //List<string> dowTickers = await momentumDbMethods.GetAllTickersAsync(IndexNames.Dow);
        //List<string> tickers = await momentumDbMethods.GetAllTickersAsync();

        List<DataFrameSim> results = await momentumDbMethods.GetPricesForTickersAsync(nasTickers);
        //Remove firms that were newly formed.
        int meanCount = (int)results.Select(x => x.ValueByDate.Count).ToList().Average();
        results = results.Where(r => r.ValueByDate.Count > meanCount)
            .OrderBy(r => r.Ticker).ToList();

        List<DataFrameSim> monthlyPercentGains = [];
        List<TickerGainByDate> yearlyPercentGains = [];
        List<TickerGainByDate> halfYearPercentGains = [];
        List<TickerGainByDate> quarterYearPercentGains = [];
        //foreach (var result in results)
        //{
        //    result.ValueByDate = (from entry in result.ValueByDate
        //                          orderby entry.Key
        //                          select entry)
        //                          .Take(300)
        //                          .ToDictionary(pair => pair.Key, pair => pair.Value);
        //}
        foreach (var result in results)
        {
            TickerGainByDate percentChangeOnDate = ComputeRollingReturn(PeriodDefinition.Annual, result);
            yearlyPercentGains.Add(percentChangeOnDate);
        }
        yearlyPercentGains = yearlyPercentGains.OrderByDescending(x => x.ChangePercent)
            .Take(50)
            .ToList();
        foreach (var result in results)
        {
            var check = yearlyPercentGains.FirstOrDefault(x => x.Ticker.Equals(result.Ticker));
            if (check is null)
            {
                continue;
            }
            TickerGainByDate percentChangeOnDate = ComputeRollingReturn(PeriodDefinition.HalfYearly, result);
            halfYearPercentGains.Add(percentChangeOnDate);
        }
        halfYearPercentGains = halfYearPercentGains.OrderByDescending(x => x.ChangePercent)
            .Take(30)
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
    }

    private TickerGainByDate ComputeRollingReturn(PeriodDefinition pd, DataFrameSim data)
    {
        double firstDateValue = data.ValueByDate.Last().Value;
        DateOnly firstDate = data.ValueByDate.Last().Key;
        DateOnly secondDate;
        switch (pd)
        {
            case PeriodDefinition.Annual:
                secondDate = firstDate.AddYears(-1);

                break;

            case PeriodDefinition.HalfYearly:
                secondDate = firstDate.AddMonths(-6);

                break;

            case PeriodDefinition.QuarterYearly:
                secondDate = firstDate.AddMonths(-3);

                break;

            default:
                return new TickerGainByDate { Ticker = data.Ticker };
        }
        secondDate = data.ValueByDate.Keys.LastOrDefault(r => r <= secondDate);
        if (secondDate == new DateOnly(1, 1, 1))
        {
            return new TickerGainByDate { Ticker = data.Ticker };
        }
        double secondValue = data.ValueByDate[secondDate];
        secondValue = ((firstDateValue / secondValue) - 1) * 100.0;
        return new TickerGainByDate { Ticker = data.Ticker, ReportedDate = firstDate, ChangePercent = secondValue };
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