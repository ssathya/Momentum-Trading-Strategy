using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;
using Polly;
using Polly.Retry;
using SecurityPriceMaintain.Services;

namespace SecurityPriceMaintain;

internal class FunctionHandler
{
    private const string ApplicationName = "SecurityPriceMaintain";
    private ILogger<FunctionHandler>? logger;
    private AsyncRetryPolicy? retryPolicy;

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
        _ = await RemoveAgedRecordsAsync(dbInterface, tickers);
        return await GetAndStorePricingValuesAsync(dbInterface, tickers);
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

    private void AppSpecificSettings(IServiceCollection services)
    {
        services.AddScoped<SecuritiesPriceDbInterface>();
        retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
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
        var endDate = DateTime.UtcNow;
        List<PriceByDate> priceList = [];
        bool returnValue = true;
        foreach (var ticker in tickers)
        {
            count++;
            IEnumerable<HistoricalData> historicDataList = [];
            if (retryPolicy is not null)
            {
                historicDataList = await retryPolicy.ExecuteAsync
                    (() => yahooClient.GetHistoricalDataAsync
                    (ticker, DataFrequency.Daily, startDate, endDate));
            }
            priceList.AddRange(historicDataList.Select(hist => PriceByDate.GeneratePriceByDate(hist, ticker)));
            if (count % 100 == 0)
            {
                returnValue = await dbInterface.StorePricingValue(priceList);
                if (returnValue == false)
                {
                    return returnValue;
                }
                priceList.Clear();
                logger?.LogInformation($"Processed {count} of {tickers.Count}");
                Thread.Sleep(100);
            }
        }
        if (priceList.Count != 0)
        {
            returnValue = await dbInterface.StorePricingValue(priceList);
            logger?.LogInformation($"Processed {count} of {tickers.Count}");
        }
        return returnValue;
    }
}