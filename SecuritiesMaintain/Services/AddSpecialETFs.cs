using Models;

namespace SecuritiesMaintain.Services;

internal interface IAddSpecialETFs
{
    Task<List<IndexComponent>> AddETFsAsync(List<IndexComponent> currentList);
}

internal class AddSpecialETFs : IAddSpecialETFs
{
    private Dictionary<string, string> indexList = new()
    {
        { "XLC", "Communication Services" },
        { "XLY", "Consumer Discretionary" },
        { "XLP", "Consumer Staples" },
        { "XLE", "Energy" },
        { "XLF", "Financials" },
        { "XLV", "Health Care" },
        { "XLI", "Industrials" },
        { "XLB", "Materials" },
        { "XLRE", "Real Estate" },
        { "XLK", "Technology" },
        { "XLU", "Utilities" },
        {"SPY", "S&P 500" },
        {"DIA", "Dow 30" },
        {"QQQ", "Nasdaq 100" }
    };

    public Task<List<IndexComponent>> AddETFsAsync(List<IndexComponent> currentList)
    {
        foreach (var etf in indexList)
        {
            if (!currentList.Any(x => x.Ticker == etf.Key))
            {
                IndexComponent newEtf = new()
                {
                    Ticker = etf.Key,
                    CompanyName = etf.Value,
                    Sector = "ETF",
                    ListedIndexes = IndexNames.Index,
                    SnPWeight = 0,
                    NasdaqWeight = 0,
                    DowWeight = 0,
                    LastUpdated = DateTime.UtcNow.Date
                };
                currentList.Add(newEtf);
            }
        }
        return Task.FromResult(currentList);
    }
}