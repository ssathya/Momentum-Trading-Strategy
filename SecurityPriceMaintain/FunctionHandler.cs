using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;
using SecurityPriceMaintain.Services;

namespace SecurityPriceMaintain;

internal class FunctionHandler
{
    private const string ApplicationName = "SecurityPriceMaintain";
    private ILogger<FunctionHandler>? logger;

    internal async Task<bool> DoApplicationProcessingAsync()
    {
        Console.WriteLine("Setting up application");
        IServiceCollection services = ServiceHandler.ConfigureServices(ApplicationName);
        AppSpecificSettings(services);
        ServiceHandler.ConnectToDb(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Console.WriteLine("Application setup complete");
        logger = provider.GetService<ILogger<FunctionHandler>>();
        SecuritiesPriceDbInterface? dbInterface = provider.GetService<SecuritiesPriceDbInterface>();
        if (dbInterface is null)
        {
            logger?.LogError("Could not generate SecuritiesPriceDbInterface object");
            return false;
        }
        List<string> tickers = [.. (await dbInterface.GetAllTickersAsync())];
        bool cleanupResult = await RemoveAgedRecordsAsync(dbInterface, tickers);
        return await GetAndStorePricingValuesAsync(dbInterface, tickers);
    }

    private static void AppSpecificSettings(IServiceCollection services)
    {
        services.AddScoped<SecuritiesPriceDbInterface>();
    }

    private static async Task<bool> RemoveAgedRecordsAsync(SecuritiesPriceDbInterface dbInterface, List<string> tickers)
    {
        var today = DateTime.UtcNow.Date;
        if (today.DayOfWeek == DayOfWeek.Wednesday) //need to pick some day; Wednesday falls in the middle of the week
        {
            return await dbInterface.DropAgedRecords(tickers);
        }
        return true;
    }

    private async Task<bool> GetAndStorePricingValuesAsync(SecuritiesPriceDbInterface dbInterface, List<string> tickers)
    {
        if (dbInterface is null)
        {
            logger?.LogError("Could not generate SecuritiesPriceDbInterface object");
            return false;
        }
        int count = 0;
        var yahooClient = new YahooClient();
        var startDate = DateTime.UtcNow.Date.AddYears(-2)
            .AddDays(-4);
        var endDate = DateTime.UtcNow.Date.AddDays(-1);
        List<PriceByDate> priceList = [];
        bool returnValue = true;
        foreach (var ticker in tickers)
        {
            count++;
            IEnumerable<HistoricalData> historicDataList = await yahooClient.GetHistoricalDataAsync(ticker, DataFrequency.Daily, startDate, endDate);
            priceList.AddRange(historicDataList.Select(hist => PriceByDate.GeneratePriceByDate(hist, ticker)));
            if (count % 25 == 0)
            {
                returnValue = await dbInterface.StorePricingValue(priceList);
                if (returnValue == false)
                {
                    return returnValue;
                }
                priceList.Clear();
                logger?.LogInformation($"Processed {count} of {tickers.Count}");
                Thread.Sleep(1000);
            }
        }
        if (priceList.Count != 0)
        {
            returnValue = await dbInterface.StorePricingValue(priceList);
        }
        return returnValue;
    }
}