using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using System.Text.RegularExpressions;

namespace SecuritiesMaintain.Services;

internal class BuildDowLst(IConfiguration configuration, ILogger<BuildDowLst> logger, HttpClient client)
: IBuildDowLst
{
    private const string tableDataTag = ".//td";
    private const string tableHeaderTag = ".//th";
    private const string tableRootNode = """//table[@class='wikitable sortable']""";
    private const string tableRowTag = ".//tr";
    private readonly HttpClient client = client;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<BuildDowLst> logger = logger;

    public async Task<List<IndexComponent>?> GetListAsync()
    {
        List<IndexComponent>? extractedValues = [];
        string? url = configuration.GetValue<string>("Dow30URL");
        if (url is null)
        {
            logger.LogError("Could not get DOW30URL URL from configuration");
            return null;
        }
        client.DefaultRequestHeaders.Add("User-Agent", "SecuritiesMaintain-1.0");
        var pageContent = await client.GetStringAsync(url);
        if (string.IsNullOrEmpty(pageContent))
        {
            logger.LogError($"Failed to get content from {url}");
            return null;
        }
        HtmlDocument doc = new();
        doc.LoadHtml(pageContent);
        var table = doc.DocumentNode.SelectSingleNode(tableRootNode);

        if (table == null)
        {
            Console.WriteLine("Extraction failed");
            return null;
        }
        var rows = table.SelectNodes(tableRowTag);
        foreach (var row in rows)
        {
            IndexComponent? returnValue = ExtractValueFromRow(row);
            if (returnValue != null)
            {
                extractedValues.Add(returnValue);
            }
        }
        return extractedValues;
    }

    private static IndexComponent? ExtractValueFromRow(HtmlNode row)
    {
        var cells = row.SelectNodes(tableDataTag);
        if (cells is not null && cells.Count > 2)
        {
            var companyCell = row.SelectSingleNode(tableHeaderTag);
            var companyName = companyCell?.InnerText.Trim();
            var ticker = cells[1].InnerText.Trim();
            string wightStr = Regex.Replace(cells[5].InnerText.Trim(), "%", "");
            _ = float.TryParse(wightStr, out float weight);
            IndexComponent ic = new()
            {
                CompanyName = companyName,
                Ticker = ticker,
                Sector = string.Empty,
                SubSector = string.Empty,
                DowWeight = weight
            };
            ic.ListedIndexes |= IndexNames.Dow;
            return ic;
        }
        return null;
    }
}