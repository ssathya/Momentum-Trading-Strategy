using AppCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using SecuritiesMaintain.Services;

namespace SecuritiesMaintain;

internal class FunctionHandler
{
    private const string ApplicationName = "SecuritiesMaintain";
    private ILogger<FunctionHandler>? logger;

    internal async Task DoApplicationProcessingAsync()
    {
        Console.WriteLine("Setting up application");
        IServiceCollection services = ServiceHandler.ConfigureServices(ApplicationName);
        AppSpecificSettings(services);
        ConnectToDb(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Console.WriteLine("Application setup complete!");
        logger = provider.GetService<ILogger<FunctionHandler>>();
        IBuildSnPLst? buildSnPLst = provider.GetService<IBuildSnPLst>();
        if (buildSnPLst is null)
        {
            logger?.LogError("Could not generate object of type IBuildSnPLst");
            return;
        }
        List<IndexComponent>? extractResult = await buildSnPLst.GetListAsync();
        if (extractResult == null)
        {
            logger?.LogError("Extracting data for S&P 500 failed");
            extractResult = [];
        }
        IBuildNasdaqLst? buildNasdaqLst = provider.GetService<IBuildNasdaqLst>();
        if (buildNasdaqLst is null)
        {
            logger?.LogError("Could not generate object of type IBuildNasdaqLst");
            return;
        }
        List<IndexComponent>? extractResult2 = await buildNasdaqLst.GetListAsync();
        if (extractResult2 is null)
        {
            logger?.LogError("Extracting data for Nasdaq failed");
            extractResult2 = [];
        }
        foreach (var item in extractResult2)
        {
            IndexComponent? existingTicker = extractResult.FirstOrDefault(x => x.Ticker == item.Ticker);
            if (existingTicker is null)
            {
                extractResult.Add(item);
            }
            else
            {
                existingTicker.ListedIndexes |= IndexNames.Nasdaq;
            }
        }
    }

    private static void AppSpecificSettings(IServiceCollection services)
    {
        services.AddScoped<IBuildSnPLst, BuildSnPLst>();
        services.AddScoped<IBuildNasdaqLst, BuildNasdaqLst>();
    }

    private static void ConnectToDb(IServiceCollection services)
    {
        IConfiguration configuration = ServiceHandler.GetConfiguration();
        string? connectionStr = configuration["SecuritiesMaintain:ConnectionString"];
        if (!string.IsNullOrEmpty(connectionStr))
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionStr);
            });
        else
            Console.WriteLine("Unable to get connection string");
    }
}