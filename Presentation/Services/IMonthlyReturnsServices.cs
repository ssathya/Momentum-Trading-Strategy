using Models.AppModels;

namespace Presentation.Services;

public interface IMonthlyReturnsServices
{
    Task<List<DateTime>> GetComputedDatesAsync();
    Task<List<TickersForDate>> GetTickersForDatesAsync();
}
