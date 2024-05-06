using AppCommon;
using Azure.Identity;
using Presentation.Components;
using Radzen;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(new Uri(@"https://momentum-trading.azconfig.io"), credential);
});
builder.Services.AddRadzenComponents();

//Database Connection
ServiceHandler.ConnectToDb(builder.Services);

//Dependency injection

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