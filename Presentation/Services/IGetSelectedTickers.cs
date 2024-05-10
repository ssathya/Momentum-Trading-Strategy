using Models;
using Models.AppModels;

namespace Presentation.Services;
public interface IGetSelectedTickers
{
    Task<List<TickerName>> GetCompanyNamesAsync(List<string> tickers);
    Task<List<PriceByDate>> GetSecurityPricesAsync(string ticker);
    Task<List<SelectedTicker>> GetSelectedTickersAsync(DateTime? selectedDate = null);
}