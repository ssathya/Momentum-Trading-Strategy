using OoplesFinance.YahooFinanceAPI.Models;

namespace Presentation.Services;

public interface ICompanyDetails
{
    Task<AssetProfile> RetrieveCompanyProfile(string symbol);
    Task<RealTimeQuoteResult> RetrieveQuotes(string symbol);
}
