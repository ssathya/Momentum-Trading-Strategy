using Microsoft.AspNetCore.Components;
using Models;
using Models.AppModels;
using Presentation.Services;
using Radzen;
using System;

namespace Presentation.Components.Pages;

public partial class CurrentSelection
{
    [Inject]
    public IGetSelectedTickers? GetSelectedTickers { get; set; }

    protected List<SelectedTicker> selectedTickers = [];

    protected override async Task OnInitializedAsync()
    {
        if (GetSelectedTickers == null)
        {
            return;
        }
        selectedTickers = await GetSelectedTickers.GetSelectedTickersAsync();
        if (selectedTickers is not null && selectedTickers.Count > 0)
        {
            List<TickerName> tickerNames = await GetSelectedTickers.GetCompanyNamesAsync(selectedTickers.Select(x => x.Ticker).ToList());
            foreach (var (selTicker, tickerName) in from selTicker in selectedTickers
                                                    let tickerName = tickerNames.FirstOrDefault(x => x.Ticker == selTicker.Ticker)
                                                    where tickerName != null
                                                    select (selTicker, tickerName))
            {
                selTicker.CompanyName = tickerName.CompanyName;
            }
        }
    }

    private void TickerSelected(SelectedTicker selTicker)
    {
        Console.WriteLine(selTicker.Ticker);
    }
}