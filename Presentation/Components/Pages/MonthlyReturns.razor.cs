using Microsoft.AspNetCore.Components;
using Models.AppModels;
using Presentation.Services;
using Radzen;

namespace Presentation.Components.Pages;

public partial class MonthlyReturns
{
    [Inject]
    public IMonthlyReturnsServices? MonthlyReturnsServices { get; set; }

    [Inject]
    public NotificationService? NotificationService { get; set; }

    protected List<TickersForDate> tickersForDates = [];
    protected string? selectedTickers = string.Empty;

    protected string assumptionMessage = "The list below does not consider security survivorship. For instance," +
        " GE Vernova (ticker GEV) was added to the S&P on April 2nd, 2024. If GEV had been in the list of " +
        "selected companies before that date, it would have been included in the computation. Our application’s " +
        "index components are updated daily, making survivorship tracking challenging. Nevertheless, we demonstrate " +
        "capital appreciation/depreciation potential using this algorithm.";

    protected override async Task OnInitializedAsync()
    {
        if (MonthlyReturnsServices != null)
        {
            tickersForDates = await MonthlyReturnsServices.GetTickersForDatesAsync();
        }
    }

    private void DateSelected(TickersForDate tickers)
    {
        selectedTickers = tickers.Tickers;
        StateHasChanged();
    }
}