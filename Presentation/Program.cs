using AppCommon;
using Azure.Identity;
using Presentation.Components;
using Presentation.Services;
using Radzen;
using Serilog;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(new Uri(@"https://momentum-trading.azconfig.io"), credential);
});
builder.Services.AddRadzenComponents();
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
    c.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    c.AddSerilog(Log.Logger);
});
Log.Logger.Information("Application Started");

//Database Connection
ServiceHandler.ConnectToDb(builder.Services);

//Dependency injection
builder.Services.AddScoped<IGetSelectedTickers, GetSelectedTickers>();
builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

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