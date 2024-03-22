using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace SecuritiesMaintain.Services;

internal class ManageIndexWeights(IConfiguration configuration, ILogger<ManageIndexWeights> logger, HttpClient client)
: IManageIndexWeights
{
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<ManageIndexWeights> logger = logger;
    private readonly HttpClient client = client;
    private const string nodesToProcess = """//div/div/table/tbody/tr""";

    //                                    """/html/body/div[2]/div[2]/div[1]/div/div/table/tbody/tr"
    private const string tableData = @"td";

    private const string tableHeader = @"<th>";

    public async Task<bool> UpdateIndexWeight(List<IndexComponent>? indices)
    {
        if (indices == null || indices.Count == 0)
        {
            return false;
        }
        string? snpWeightUrl = configuration.GetValue<string>("SecuritiesMaintain:SNPWeight");
        string? nasdaqWeightUrl = configuration.GetValue<string>("SecuritiesMaintain:NasdaqWeight");
        string? dow30WeightUrl = configuration.GetValue<string>("SecuritiesMaintain:DowWeight");

        if (string.IsNullOrEmpty(snpWeightUrl))
        {
            logger.LogError("SNPWeight url is null");
            return false;
        }
        client.DefaultRequestHeaders.Add("User-Agent", "Other");
        bool updateResult = true;
        updateResult = await PopulateIndexValues(indices, IndexNames.SnP, snpWeightUrl);
        if (!updateResult)
        {
            logger.LogError($"Error fetching SnP-500 index weights; URL:{snpWeightUrl}");
            return false;
        }
        updateResult = await PopulateIndexValues(indices, IndexNames.Nasdaq, nasdaqWeightUrl);
        if (!updateResult)
        {
            logger.LogError($"Error fetching NASDAQ-100 index weights; URL:{nasdaqWeightUrl}");
            return false;
        }
        updateResult = await PopulateIndexValues(indices, IndexNames.Dow, dow30WeightUrl);
        if (!updateResult)
        {
            logger.LogError($"Error fetching Dow-30 index weights; URL:{dow30WeightUrl}");
        }
        return updateResult;
    }

    private async Task<bool> PopulateIndexValues(List<IndexComponent> indices, IndexNames indexName, string? weightURL)
    {
        var pageContent = await client.GetStringAsync(weightURL);
        if (string.IsNullOrEmpty(pageContent))
        {
            logger.LogError("SNPWeight pageContent is null");
            return false;
        }
        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(pageContent);
        HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes(nodesToProcess);
        if (nodes.Count == 0)
        {
            logger.LogError($"Parsing error; URL = {weightURL}");
            return false;
        }
        foreach (var node in nodes)
        {
            if (node.InnerHtml.Contains(tableHeader, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.LogInformation($"Skipping row {node.InnerHtml}");
                continue;
            }
            bool extractResult = ExtractDataFromNode(out string? ticker, out float indexWeight, node);
            if (!string.IsNullOrEmpty(ticker))
            {
                var index = indices.FirstOrDefault(x => x.Ticker == ticker || x.Ticker == ticker.Replace('-', '.'));
                if (index != null)
                {
                    switch (indexName)
                    {
                        case IndexNames.SnP:
                            index.SnPWeight = indexWeight;
                            break;

                        case IndexNames.Nasdaq:
                            index.NasdaqWeight = indexWeight;
                            break;

                        case IndexNames.Dow:
                            index.DowWeight = indexWeight;
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        return true;
    }

    private static bool ExtractDataFromNode(out string? ticker, out float indexWeight, HtmlNode node)
    {
        int index = 0;
        ticker = "";
        indexWeight = 0;

        foreach (HtmlNode col in node.SelectNodes(tableData))
        {
            switch (index)
            {
                case 2:
                    ticker = col.InnerText;
                    break;

                case 3:
                    indexWeight = float.TryParse(col.InnerText.Replace("%", ""), out float result) ? result : 0;
                    break;

                default:
                    break;
            }
            index++;
        }

        return index >= 5;
    }
}