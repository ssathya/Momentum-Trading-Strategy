using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComputeMomentum;

internal class FunctionHandler
{
    private const string ApplicationName = "ComputeMomentum";
    private ILogger<FunctionHandler>? logger;

    internal Task DoApplicationProcessingAsync()
    {
        Console.WriteLine("Setting up application");
        IServiceCollection services = ServiceHandler.ConfigureServices(ApplicationName);
        AppSpecificSettings(services);
        ServiceHandler.ConnectToDb(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Console.WriteLine("Application setup complete");
        logger = provider.GetService<ILogger<FunctionHandler>>();
        return Task.CompletedTask;
    }

    private void AppSpecificSettings(IServiceCollection services)
    {
    }
}