using Microsoft.Extensions.Caching.Distributed;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Models;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace Presentation.Services;

public class CompanyDetails(IDistributedCache cache, ILogger<CompanyDetails> logger) : ICompanyDetails
{
    private readonly IDistributedCache cache = cache;
    private readonly ILogger<CompanyDetails> logger = logger;
    private readonly YahooClient? yahooClient = new();
    private readonly AsyncRetryPolicy retryPolicy = CreateRetryPolicy();

    public async Task<AssetProfile> RetrieveCompanyProfile(string symbol)
    {
        string cacheKey = $"{symbol}-profile";
        AssetProfile profile;
        try
        {
            profile = JsonSerializer.Deserialize<AssetProfile>(cache.GetString(cacheKey) ?? "") ?? new AssetProfile();
        }
        catch
        {
            profile = new AssetProfile();
        }
        if (!string.IsNullOrEmpty(profile.LongBusinessSummary))
        {
            return profile;
        };
        if (yahooClient != null)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                profile = await yahooClient.GetAssetProfileAsync(symbol);
            });
            string profileSerialized = JsonSerializer.Serialize(profile);
            cache.SetString(cacheKey, profileSerialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(10)
            });
        }
        else
        {
            logger.LogCritical("Application is not configured to use Yahoo Finance API");
        }
        return profile;
    }

    public async Task<RealTimeQuoteResult> RetrieveQuotes(string symbol)
    {
        string cacheKey = $"{symbol}-quotes";
        RealTimeQuoteResult? quotes;
        try
        {
            quotes = JsonSerializer.Deserialize<RealTimeQuoteResult>(cache.GetString(cacheKey) ?? "");
        }
        catch
        {
            quotes = null;
        }
        if (quotes != null && !string.IsNullOrEmpty(quotes.FiftyTwoWeekRange))
        {
            return quotes;
        }
        if (yahooClient != null)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                quotes = await yahooClient.GetRealTimeQuotesAsync(symbol);
            });
            string quotesSerialized = JsonSerializer.Serialize(quotes);
            cache.SetString(cacheKey, quotesSerialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        }
        else
        {
            logger.LogCritical("Application is not configured to use Yahoo Finance API");
        }
        return quotes ?? new RealTimeQuoteResult();
    }

    private static AsyncRetryPolicy CreateRetryPolicy()
    {
        return Policy
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(5, retryAttempt)));
    }
}