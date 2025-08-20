using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace SecuritiesMaintain.Services;

internal class BuildSnPLst(IConfiguration configuration, ILogger<BuildSnPLst> logger, HttpClient client) : IBuildSnPLst
{
    private const string nodeEleToProcess = """//*[@id="constituents"]/tbody/tr""";
    private const string tableData = @"td";
    private const string tableHeader = @"<th>";
    private readonly HttpClient client = client;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<BuildSnPLst> logger = logger;

    public async Task<List<IndexComponent>?> GetListAsync()
    {
        List<IndexComponent> extractedValues = [];
        string? url = configuration.GetValue<string>("SNP500URL");
        if (url is null)
        {
            logger.LogError("Could not get S&P-500 URL for processing");
            return null;
        }
        client.DefaultRequestHeaders.Add("User-Agent", "Chrome/111.0.0.0");
        var pageContent = await client.GetStringAsync(url);
        if (string.IsNullOrEmpty(pageContent))
        {
            logger.LogError($"Fetch data failed; URL = {url}");
            return null;
        }
        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(pageContent);
        HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes(nodeEleToProcess);
        if (nodes.Count == 0)
        {
            logger.LogWarning($"Parsing error; URL = {url}");
            return null;
        }
        foreach (var node in nodes)
        {
            if (node.InnerHtml.Contains(tableHeader, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.LogInformation($"Skipping row {node.InnerHtml}");
                continue;
            }
            IndexComponent component = ExtractFirmFromNode(node);
            extractedValues.Add(component);
        }
        return extractedValues;
    }

    private static IndexComponent ExtractFirmFromNode(HtmlNode node)
    {
        int index = 0;
        IndexComponent component = new();
        foreach (HtmlNode col in node.SelectNodes(tableData))
        {
            switch (index)
            {
                case 0:
                    component.Ticker = col.InnerText;
                    break;

                case 1:
                    component.CompanyName = col.InnerText;
                    break;

                case 2:
                    component.Sector = col.InnerText;
                    break;

                case 3:
                    component.SubSector = col.InnerText;
                    break;

                default:
                    break;
            }
            index++;
        }
        component.CleanUpValues();
        component.ListedIndexes |= IndexNames.SnP;
        return component;
    }
}