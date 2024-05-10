using AppCommon.NYSECalendar.Compute;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.AppModels;

namespace Presentation.Services;

public class GetSelectedTickers(ILogger<GetSelectedTickers> logger, IDbContextFactory<AppDbContext> contextFactory) : IGetSelectedTickers
{
    private readonly ILogger<GetSelectedTickers> logger = logger;
    private readonly IDbContextFactory<AppDbContext> contextFactory = contextFactory;

    public async Task<List<SelectedTicker>> GetSelectedTickersAsync(DateTime? selectedDate = null)
    {
        using var context = contextFactory.CreateDbContext();

        try
        {
            if (selectedDate == null)
            {
                selectedDate = context.SelectedTickers.Max(x => x.Date);
            }
            else
            {
                var tradingDay = TradingCalendar.GetTradingDay(selectedDate.Value);
                if (tradingDay.BusinessDay == false)
                {
                    return [];
                }
            }
            var selectedTickers = await (from st in context.SelectedTickers
                                         where st.Date == selectedDate
                                         select st)
                        .ToListAsync();

            return selectedTickers;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting selected tickers");
            return [];
        }
    }

    public async Task<List<PriceByDate>> GetSecurityPricesAsync(string ticker)
    {
        using var context = contextFactory.CreateDbContext();
        try
        {
            List<PriceByDate> pricesByDate = await context.PriceByDate
                .Where(r => r.Ticker == ticker).ToListAsync();
            return pricesByDate;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error pulling prices for {ticker}: {ex}");
            return [];
        }
    }

    public async Task<List<TickerName>> GetCompanyNamesAsync(List<string> tickers)
    {
        using var context = contextFactory.CreateDbContext();
        try
        {
            List<TickerName> result = [];
            foreach (var ticker in await context.IndexComponents.Select(i => new { i.Ticker, i.CompanyName })
                .Where(t => tickers.Contains(t.Ticker))
                .ToListAsync())
            {
                result.Add(new TickerName()
                {
                    Ticker = ticker.Ticker,
                    CompanyName = ticker.CompanyName
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error getting Company Name for selected tickers: {ex}");
            return [];
        }
    }
}