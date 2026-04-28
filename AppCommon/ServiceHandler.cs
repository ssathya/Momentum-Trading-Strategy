using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Serilog;
using System.Net;
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
        //services.AddScoped<HttpClient>();
        services.AddHttpClient("BrowserClient")
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                })
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                client.DefaultRequestHeaders.Add(
                    "Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            });

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
            .AddSystemsManager("/Momentum", TimeSpan.FromMinutes(5));
        //.AddAzureAppConfiguration(options =>
        //{
        //    //options.Connect(configProvider);
        //    options.Connect(new Uri(@"https://momentum-trading.azconfig.io"), credential);
        //    // Wrong code. look into http://tinyurl.com/AzureAppConfig for more details.
        //    //options.ConfigureRefresh(refresh =>
        //    //{                                                                                                                                       //    refresh.Register(configProvider)                                                                                                    //       .SetCacheExpiration(TimeSpan.FromSeconds(10));                                                                                   //});                                                                                                                                 });                                                                                                                                   Configuration = builder.Build();                                                                                                          return Configuration;                                                                                                                 }
        //});
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

        string? connectionStr = configuration["ConnectionString"];
        if (string.IsNullOrEmpty(connectionStr))
        {
            Console.WriteLine("Unable to get connection strings");
            return;
        }
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