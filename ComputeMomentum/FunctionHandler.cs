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
        Computation? computation = provider.GetService<Computation>();

        if (computation == null)
        {
            logger?.LogError("Unable to create an instance of Computation");
            return null;
        }
        List<DataFrameSim> results = await computation.ObtainTickersAndClosingPricesAsync();
        //Remove firms that were newly formed.
        int meanCount = (int)results.Select(x => x.ValueByDate.Count).ToList().Average();
        results = [.. results.Where(r => r.ValueByDate.Count > meanCount).OrderBy(r => r.Ticker)];

        List<SelectedTicker> selectedTickers = [];
        computation.ComputeSelectedTickersForPeriod(results, selectedTickers);
        ComputeForPreviousMonths(computation, results, selectedTickers);
        DateTime currentDate = DateTime.UtcNow.Date;
        foreach (var selectedTicker in selectedTickers)
        {
            selectedTicker.LastUpdated = currentDate;
        }
        MaintainComputeValues? maintainComputeValues = provider.GetService<MaintainComputeValues>();
        if (maintainComputeValues != null)
        {
            await maintainComputeValues.DeleteAgedRecords();
            await maintainComputeValues.UpdateSelectedPositions(selectedTickers);
        }
        return selectedTickers;
    }

    private static void ComputeForPreviousMonths(Computation computation, List<DataFrameSim> results,
        List<SelectedTicker> selectedTickers)
    {
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
            computation.ComputeSelectedTickersForPeriod(results, selectedTickers);
            //NYSE is not closed for more than 3 days in a stretch.
            fTD = fTD.AddDays(-4);
            RemoveForwardPrices(results, fTD);
        }
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

    private void AppSpecificSettings(IServiceCollection services)
    {
        services.AddScoped<MomentumDbMethods>();
        services.AddScoped<MaintainComputeValues>();
        services.AddScoped<Computation>();
    }
}