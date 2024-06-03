using AppCommon.NYSECalendar.Compute;
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
    protected TickersForDate selectedTickersForDate = new();
    protected bool hasUserSelected = false;
    protected string? messageToDisplay = string.Empty;

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
            tickersForDates = [.. tickersForDates.OrderBy(t => t.Date)];
        }
    }

    private async Task DateSelectedAsync(TickersForDate tickersForDate)
    {
        selectedTickersForDate = tickersForDate;
        DateTime startingDate = selectedTickersForDate.Date;
        hasUserSelected = true;
        StateHasChanged();
        if (MonthlyReturnsServices == null) return;
        bool continueLoop = true;
        double cumulativeReturn = 100000.0;
        do
        {
            List<VirtualReturns> virtualReturns = await MonthlyReturnsServices.GetPricesForGivenMonthAsync(selectedTickersForDate, cumulativeReturn);
            cumulativeReturn = virtualReturns.Sum(r => r.GainLoss) + cumulativeReturn;
            var tmpTickersForDate = tickersForDates.FirstOrDefault(t => t.Date > selectedTickersForDate.Date);
            if (tmpTickersForDate == null)
            {
                continueLoop = false;
                continue;
            }
            var selectedDate = tmpTickersForDate.Date;
            if (TradingCalendar.FirstTradingDayOfMonth(selectedDate.Month, selectedDate.Year).Date != selectedDate.Date)
            {
                continueLoop = false;
                continue;
            }
            selectedTickersForDate = tmpTickersForDate;
        } while (continueLoop);
        messageToDisplay = $"Assuming you invested $100,000 in the index on {startingDate.ToShortDateString()}, your capital would be around {cumulativeReturn:C0} on {selectedTickersForDate.Date.ToShortDateString()}. ";
    }
}