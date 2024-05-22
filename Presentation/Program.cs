using AppCommon;
using NeoSmart.Caching.Sqlite;
using NeoSmart.Caching.Sqlite.AspNetCore;
using Presentation.Components;
using Presentation.Services;
using Radzen;
using Serilog;
using System.Globalization;
using System.Text;

CultureInfo cultureInfo = new("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRadzenComponents();

//Get connection string from AWS System Manager
builder.Configuration.AddSystemsManager("/Momentum", TimeSpan.FromMinutes(5));
//Logger
IConfiguration configuration = builder.Configuration;

StringBuilder filePath = new();
filePath.Append(Path.GetTempPath() + "/");
filePath.Append("Presentation-.log");
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(filePath.ToString(),
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 3)
    .CreateLogger();
builder.Services.AddLogging(c =>
{
    c.SetMinimumLevel(LogLevel.Information);
    c.AddSerilog(Log.Logger);
});
Log.Logger.Information("Application Started");

//Database Connection
ServiceHandler.ConnectToDb(builder.Services, configuration["ConnectionString"] ?? string.Empty);

//Dependency injection
builder.Services.AddScoped<IGetSelectedTickers, GetSelectedTickers>();
builder.Services.AddScoped<ICompanyDetails, CompanyDetails>();
//builder.Services.AddHttpClient();

builder.Services.AddSqliteCache(options =>
{
    options.CachePath = Path.Combine(Path.GetTempPath(), "Momentum-cache.db");
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new[] {
        cultureInfo
    },
    SupportedUICultures = new[] {
       cultureInfo
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();