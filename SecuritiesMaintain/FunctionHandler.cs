﻿using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using SecuritiesMaintain.Services;

namespace SecuritiesMaintain;

internal class FunctionHandler
{
    private const string ApplicationName = "SecuritiesMaintain";
    private ILogger<FunctionHandler>? logger;

    internal async Task<List<IndexComponent>?> DoApplicationProcessingAsync()
    {
        Console.WriteLine("Setting up application");
        IServiceCollection services = ServiceHandler.ConfigureServices(ApplicationName);
        AppSpecificSettings(services);
        ServiceHandler.ConnectToDb(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Console.WriteLine("Application setup complete!");
        logger = provider.GetService<ILogger<FunctionHandler>>();
        GetFetchObjects(provider
            , out IBuildSnPLst? buildSnPLst
            , out IBuildNasdaqLst? buildNasdaqLst
            , out IBuildDowLst? buildDowLst);
        IManageIndexWeights? indexWeights = provider.GetService<IManageIndexWeights>();
        if (buildSnPLst is null || buildDowLst is null || buildNasdaqLst is null)
        {
            logger?.LogError("Could not generate object of type IBuildSnPLst or IBuildDowLst or IBuildNasdaqLst");
            return null;
        }
        if (indexWeights is null)
        {
            logger?.LogError("Could not generate object of type IManageSnPWeights");
            return null;
        };
        List<IndexComponent>? extractResult = await buildSnPLst.GetListAsync();
        List<IndexComponent>? extractResult2 = await buildNasdaqLst.GetListAsync();
        List<IndexComponent>? extractResult3 = await buildDowLst.GetListAsync();
        Task.WaitAll();
        if (extractResult is null || extractResult2 is null || extractResult3 is null)
        {
            logger?.LogError("Extracting data for S&P 500 or Nasdaq or Dow failed");
            return null;
        }

        MergeExtracts(extractResult, extractResult2, extractResult3);
        await indexWeights.UpdateIndexWeight(extractResult);
        IIndexToDbService? indexToDbService = provider.GetService<IIndexToDbService>();
        if (indexToDbService is not null)
        {
            _ = await indexToDbService.SelectCurrentIndexCountAsync();
            await indexToDbService.UpdateIndexList(extractResult);
            await indexToDbService.DeleteAgedRecords();
        }
        return extractResult;
    }

    private static void MergeExtracts(List<IndexComponent> extractResult
        , List<IndexComponent> extractResult2
        , List<IndexComponent> extractResult3)
    {
        foreach (var (item, existingTicker) in from item in extractResult2
                                               let existingTicker = extractResult.FirstOrDefault(x => x.Ticker == item.Ticker)
                                               select (item, existingTicker))
        {
            AddIndexElementToExtractResult(extractResult, existingTicker, item, IndexNames.Nasdaq);
        }
        foreach (var (item, existingTicker) in from item in extractResult3
                                               let existingTicker = extractResult.FirstOrDefault(x => x.Ticker == item.Ticker)
                                               select (item, existingTicker))
        {
            AddIndexElementToExtractResult(extractResult, existingTicker, item, IndexNames.Dow);
        }
        foreach (var extractResultItem in extractResult)
        {
            extractResultItem.LastUpdated = DateTime.UtcNow.Date;
        }
    }

    private static void GetFetchObjects(ServiceProvider provider, out IBuildSnPLst? buildSnPLst, out IBuildNasdaqLst? buildNasdaqLst, out IBuildDowLst? buildDowLst)
    {
        buildSnPLst = provider.GetService<IBuildSnPLst>();
        buildNasdaqLst = provider.GetService<IBuildNasdaqLst>();
        buildDowLst = provider.GetService<IBuildDowLst>();
    }

    private static void AddIndexElementToExtractResult(List<IndexComponent> extractResult, IndexComponent existingTicker, IndexComponent item, IndexNames indexName)
    {
        if (existingTicker is null)
        {
            extractResult.Add(item);
        }
        else
        {
            existingTicker.ListedIndexes |= indexName;
        }
    }

    private static void AppSpecificSettings(IServiceCollection services)
    {
        services.AddScoped<IBuildSnPLst, BuildSnPLst>();
        services.AddScoped<IBuildNasdaqLst, BuildNasdaqLst>();
        services.AddScoped<IBuildDowLst, BuildDowLst>();
        services.AddScoped<IIndexToDbService, IndexToDbService>();
        services.AddScoped<IManageIndexWeights, ManageIndexWeights>();
    }
}