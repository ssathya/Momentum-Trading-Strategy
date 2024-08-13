using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;

namespace SecuritiesMaintain.Services;

internal class BuildNasdaqLst(IConfiguration configuration, ILogger<BuildSnPLst> logger, HttpClient client) : IBuildNasdaqLst
{
    //private const string nodeEleToProcess = """//*[@id='constituents']/tbody/tr""";
    private const string nodeEleToProcess = """//*/table[5]/tbody/tr""";

    private const string tableDataTag = "td";
    private const string tableHeaderTag = "<th>";
    private readonly HttpClient client = client;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<BuildSnPLst> logger = logger;

    public async Task<List<IndexComponent>?> GetListAsync()
    {
        List<IndexComponent>? extractedValues = [];
        string? url = configuration.GetValue<string>("Nasdaq100URL");
        if (url is null)
        {
            logger.LogError("Could not get NASDAQ100 URL from configuration");
            return null;
        }
        var pageContent = await client.GetStringAsync(url);
        if (string.IsNullOrEmpty(pageContent))
        {
            logger.LogError($"Failed to get content from {url}");
            return null;
        }
        HtmlDocument htmlDocument = new();
        htmlDocument.LoadHtml(pageContent);
        HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes(nodeEleToProcess);
        if (nodes == null || nodes.Count == 0)
        {
            logger.LogCritical($"Parsing error; URL = {url}");
            return null;
        }
        foreach (var node in nodes)
        {
            if (node.InnerHtml.Contains(tableHeaderTag, StringComparison.InvariantCultureIgnoreCase))
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
        foreach (HtmlNode col in node.SelectNodes(tableDataTag))
        {
            switch (index)
            {
                case 0:
                    component.CompanyName = col.InnerText;
                    break;

                case 1:
                    component.Ticker = col.InnerText;
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
        component.ListedIndexes |= IndexNames.Nasdaq;
        return component;
    }
}