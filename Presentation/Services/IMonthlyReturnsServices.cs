using Models;
using Models.AppModels;

namespace Presentation.Services;

public interface IMonthlyReturnsServices
{
    Task<List<VirtualReturns>> GetPricesForGivenMonthAsync(TickersForDate tickersForDate, double totalFunds);

    Task<List<DateTime>> GetComputedDatesAsync();

    Task<List<TickersForDate>> GetTickersForDatesAsync();
}