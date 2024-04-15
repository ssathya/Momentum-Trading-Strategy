// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Models;
using System.Reflection;

Console.WriteLine("Hello, World!");
IConfiguration configuration = BuildConfig();
var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(x => x.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: false))
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql((configuration["ConnectionString:DefaultConnection"]));
        });
    })
    .Build();

Console.WriteLine("This application does nothing; used for model creation");
static IConfigurationRoot BuildConfig()
{
    var builder = new ConfigurationBuilder();
    IConfigurationRoot configuration = builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: false)
        .Build();
    var LoopTimes = configuration["LoopTimes"];
    return configuration;
}