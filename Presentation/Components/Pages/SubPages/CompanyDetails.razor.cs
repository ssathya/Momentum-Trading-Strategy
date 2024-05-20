using Microsoft.AspNetCore.Components;
using Models.AppModels;
using OoplesFinance.YahooFinanceAPI.Models;
using Presentation.Services;

namespace Presentation.Components.Pages.SubPages;

public partial class CompanyDetails
{
    [Parameter]
    public string? Ticker { get; set; } = string.Empty;

    [Inject]
    public ICompanyDetails? CDService { get; set; }

    [Inject]
    public IGetSelectedTickers? SelectedTickers { get; set; }

    protected string? CompanyName { get; set; }
    protected string? CompanyDescription { get; set; }

    protected AssetProfile? CompanyProfile { get; set; }
    protected RTCQResult RealTimeQuote { get; set; } = new();

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Ticker) || CDService == null)
        {
            return;
        }
        CompanyProfile = await CDService.RetrieveCompanyProfile(Ticker);
        CompanyDescription = CompanyProfile?.LongBusinessSummary;
        await PopulateCompanyName();
        await GetRealTimeQuoteResult();
        if (string.IsNullOrEmpty(RealTimeQuote.DisplayName))
        {
            RealTimeQuote.DisplayName = CompanyName ?? "";
        }
    }

    private async Task PopulateCompanyName()
    {
        if (SelectedTickers != null)
        {
            List<string> tickers = [Ticker ?? ""];
            TickerName? tickerName = (await SelectedTickers.GetCompanyNamesAsync(tickers))
                .FirstOrDefault();
            if (tickerName != null)
            {
                CompanyName = tickerName.CompanyName;
            }
        }
        if (string.IsNullOrEmpty(CompanyName))
        {
            CompanyName = Ticker;
        }
    }

    private async Task GetRealTimeQuoteResult()
    {
        if (string.IsNullOrEmpty(Ticker) || CDService == null)
        {
            return;
        }
        RealTimeQuote = await CDService.RetrieveQuotes(Ticker);
    }
}