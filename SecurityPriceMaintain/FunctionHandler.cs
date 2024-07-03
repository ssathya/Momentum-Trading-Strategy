using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using NodaTime;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;
using OoplesFinance.YahooFinanceAPI.Models;
using Polly;
using Polly.Retry;
using SecurityPriceMaintain.Services;
using YahooQuotesApi;

namespace SecurityPriceMaintain;

internal class FunctionHandler
{
    private const string ApplicationName = "SecurityPriceMaintain";
    private ILogger<FunctionHandler>? logger;
    private AsyncRetryPolicy? retryPolicy;
    private List<string> tickersWithErrors = [];

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
        bool returnValue = await GetAndStorePricingValuesAsync(dbInterface, tickers);
        if (tickersWithErrors.Count != 0)
        {
            returnValue = await GetAndStorePricingValuesYQAsync(dbInterface, tickersWithErrors)
                && returnValue;
        }
        return returnValue;
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

    private async Task<bool> GetAndStorePricingValuesYQAsync(SecuritiesPriceDbInterface dbInterface, List<string> tickers)
    {
        if (dbInterface is null)
        {
            logger?.LogError("Could not generate SecuritiesPriceDbInterface object");
            return false;
        }
        int count = 0;
        int errorCount = 0;
        var startDate = DateTime.UtcNow.Date.AddYears(-2)
            .AddDays(-4);
        YahooQuotes yahooQuotes = new YahooQuotesBuilder()
            .WithHistoryStartDate(Instant.FromUtc(startDate.Year, startDate.Month, startDate.Day, 0, 0))
            .WithCacheDuration(snapshotCacheDuration: Duration.FromMinutes(30), historyCacheDuration: Duration.FromHours(6))
            .Build();
        bool returnValue = true;

        List<PriceByDate> priceList = [];
        foreach (var ticker in tickers)
        {
            count++;
            Security? security = await yahooQuotes.GetAsync(ticker, Histories.PriceHistory);
            if (security == null)
            {
                logger?.LogError($"Could not get pricing data for {ticker}");
                errorCount++;
                continue;
            }
            if (security.PriceHistory.HasError)
            {
                logger?.LogError($"Error getting pricing data for {ticker}: {security.PriceHistory.Error}");
                errorCount++;
                continue;
            }
            PriceTick tick = security.PriceHistory.Value[0];
            PriceTick[] values = security.PriceHistory.Value;
            foreach (var value in values)
            {
                priceList.Add(PriceByDate.GeneratePriceByDate(value, ticker));
            }
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
        if (errorCount >= 5)
        {
            logger?.LogError($"Too many errors; Error count = {errorCount}");
        }
        return returnValue;
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
            try
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
            catch (Exception ex)
            {
                logger?.LogCritical(ex, $"Error processing {ticker}");
                tickersWithErrors.Add(ticker);
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