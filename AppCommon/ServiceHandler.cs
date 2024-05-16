using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Serilog;
using System.Text;

namespace AppCommon;

public static class ServiceHandler
{
    private static IConfiguration? Configuration;

    public static IServiceCollection ConfigureServices(string applicationName)
    {
        IServiceCollection services = new ServiceCollection();
        Configuration = BuildConfiguration();
        services.AddSingleton<IConfiguration>(_ => Configuration!);
        services.AddScoped<HttpClient>();
        //Setup db connection
        //SetupDatabaseConnection(services, Configuration);
        //string? value = Configuration?.GetValue<string>(applicationName);
        //Logger
        SetupLogger(services, Configuration!, applicationName);
        return services;
    }

    public static IConfiguration GetConfiguration()
    {
        Configuration ??= BuildConfiguration();
        return Configuration!;
    }

    private static IConfiguration? BuildConfiguration()
    {
        //string configProvider = @"Sensitive information will be here!";
        var credential = new DefaultAzureCredential();
        ConfigurationBuilder builder = new();
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json"
                , optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddAzureAppConfiguration(options =>
            {
                //options.Connect(configProvider);
                options.Connect(new Uri(@"https://momentum-trading.azconfig.io"), credential);
                // Wrong code. look into http://tinyurl.com/AzureAppConfig for more details.
                //options.ConfigureRefresh(refresh =>
                //{                                                                                                                                       //    refresh.Register(configProvider)                                                                                                    //       .SetCacheExpiration(TimeSpan.FromSeconds(10));                                                                                   //});                                                                                                                                 });                                                                                                                                   Configuration = builder.Build();                                                                                                          return Configuration;                                                                                                                 }
            });
        Configuration = builder.Build();

        return Configuration;
    }

    private static void SetupLogger(IServiceCollection services, IConfiguration configuration, string applicationName)
    {
        StringBuilder filePath = new();
        filePath.Append(Path.GetTempPath() + "/");
        filePath.Append($"{applicationName}-.log");
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(filePath.ToString(),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 3)
            .CreateLogger();
        services.AddLogging(c =>
        {
            c.SetMinimumLevel(LogLevel.Information);
            c.AddSerilog(Log.Logger);
        });
        Log.Logger.Information("Application starting...");
    }

    public static void ConnectToDb(IServiceCollection services)
    {
        IConfiguration configuration = GetConfiguration();

        string? connectionStrCombined = configuration["SecuritiesMaintain:ConnectionString"];
        if (string.IsNullOrEmpty(connectionStrCombined))
        {
            Console.WriteLine("Unable to get connection strings");
            return;
        }
        string[] connectionStrs = connectionStrCombined.Split('|');
        string connectionStr = Environment.MachineName
            .Contains("-DELL", StringComparison.InvariantCultureIgnoreCase) ?
            connectionStrs[1] ?? "" : connectionStrs[0] ?? "";

        ConnectToDb(services, connectionStr);
    }

    public static void ConnectToDb(IServiceCollection services, string connectionStr)
    {
        if (!string.IsNullOrEmpty(connectionStr))
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionStr);
            });
        else
            Console.WriteLine("Unable to get connection string");
    }
}