using AppCommon.NYSECalendar.Compute;
using FluentDateTime;
using Microsoft.AspNetCore.Components;
using Models;
using Models.AppModels;
using Presentation.Services;
using System.Globalization;

namespace Presentation.Components.Pages.SubPages;

public partial class TickerChart
{
    protected int currentSelect = 0;

    [Inject]
    public IGetSelectedTickers? SelectedTickers { get; set; }

    [Parameter]
    public string? Ticker { get; set; } = string.Empty;

    protected List<PriceByDate>? PriceByDates { get; set; }
    protected TickerName? TickerName { get; set; }
    protected List<PriceByDate>? ValuesFromDb { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(Ticker) || SelectedTickers is null)
        {
            return;
        }
        ValuesFromDb = await SelectedTickers.GetSecurityPricesAsync(Ticker);
        if (ValuesFromDb.Count == 0)
        {
            return;
        }
        TickerName = (await SelectedTickers.GetCompanyNamesAsync([Ticker]))
            .FirstOrDefault();
        TickerName ??= new()
        {
            CompanyName = Ticker,
            Ticker = Ticker
        };
        ExtractRecordsToChart();
    }

    private void ExtractRecordsToChart()
    {
        if (ValuesFromDb is null || ValuesFromDb.Count == 0)
        {
            return;
        }
        PriceByDates = ValuesFromDb.ToList();
        PriceByDates = [.. PriceByDates.OrderBy(p => p.Date)];
        PriceByDate lastPrice = PriceByDates.Last();
        PriceByDate firstPrice;
        if (currentSelect == 0 || currentSelect == 1)
        {
            firstPrice = PriceByDates.First();
        }
        else
        {
            int recordCount = PriceByDates.Count;
            firstPrice = PriceByDates.Skip(recordCount / 4).First();
        }
        DateTime refDate = firstPrice.Date;
        List<PriceByDate> firstTradingDaysOfMonth = [];
        do
        {
            var a = PriceByDates.Find(p => p.Date == refDate.Date);
            if (a is not null)
            {
                firstTradingDaysOfMonth.Add(a);
            }
            else
            {
                break;
            }
            DateTime nextPeriodRef;
            if (currentSelect == 0 || currentSelect == 1)
            {
                nextPeriodRef = refDate.AddMonths(1);
                refDate = TradingCalendar.FirstTradingDayOfMonth(nextPeriodRef.Month, nextPeriodRef.Year);
            }
            else
            {
                refDate = refDate.AddDays(8);
                nextPeriodRef = refDate.Previous(DayOfWeek.Monday);
                DateTime limitDate = nextPeriodRef.Next(DayOfWeek.Friday);

                refDate = TradingCalendar.GetTradingDays(nextPeriodRef, limitDate).First()
                    .Date;
            }

            if (firstPrice.Date >= lastPrice.Date)
            {
                break;
            }
        } while (true);
        firstTradingDaysOfMonth.Add(lastPrice);
        firstTradingDaysOfMonth = firstTradingDaysOfMonth.Distinct().ToList();
        switch (currentSelect)
        {
            case 0:
                PriceByDates = firstTradingDaysOfMonth;
                break;

            case 1:
                DateTime startDate = lastPrice.Date.AddYears(-1);
                PriceByDates = firstTradingDaysOfMonth.Where(r => r.Date >= startDate)
                    .ToList();
                break;

            case 2:
                startDate = lastPrice.Date.AddMonths(-6);
                PriceByDates = firstTradingDaysOfMonth.Where(r => r.Date >= startDate)
                    .ToList();
                break;

            case 3:
                startDate = lastPrice.Date.AddMonths(-3);
                PriceByDates = firstTradingDaysOfMonth.Where(r => r.Date >= startDate)
                    .ToList();
                break;
        }
    }

    private string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    private void PeriodChange(int value)
    {
        currentSelect = value;
        ExtractRecordsToChart();
    }
}