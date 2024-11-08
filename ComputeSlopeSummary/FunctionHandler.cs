using AppCommon;
using ComputeSlopeSummary.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComputeSlopeSummary;

internal class FunctionHandler
{
    private const string ApplicationName = "ComputeSlopeSummary";
    private ILogger<FunctionHandler>? logger;

    internal async Task DoApplicationProcessingAsync()
    {
        Console.WriteLine("Setting up application");
        IServiceCollection services = ServiceHandler.ConfigureServices(ApplicationName);
        AppSpecificSettings(services);
        ServiceHandler.ConnectToDb(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Console.WriteLine("Application setup complete");
        logger = provider.GetRequiredService<ILogger<FunctionHandler>>();
        ComputeSummaries? computeSummaries = provider.GetRequiredService<ComputeSummaries>();
        if (computeSummaries != null)
        {
            bool wasSuccessful = await computeSummaries.GenerateSummaries();
            if (!wasSuccessful)
            {
                logger.LogError("Failed to generate summaries");
                return;
            }
        }
        GenerateExcel? generateExcel = provider.GetRequiredService<GenerateExcel>();
        if (generateExcel != null)
        {
            bool wasSuccessful = await generateExcel.GenerateExcelAsync();
            if (!wasSuccessful)
            {
                logger.LogError("Failed to generate excel s/s");
            }
            await generateExcel.RecursiveSelection();
        }
    }

    private void AppSpecificSettings(IServiceCollection services)
    {
        services.AddScoped<ComputeSummaries>();
        services.AddScoped<SummaryDbMethods>();
        services.AddScoped<GenerateExcel>();
    }
}