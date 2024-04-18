// See https://aka.ms/new-console-template for more information
using SecuritiesMaintain;

Console.WriteLine("Getting index securities");
var fh = new FunctionHandler();
var indexComponents = await fh.DoApplicationProcessingAsync();
Console.WriteLine($"Covering {indexComponents?.Count} firms");
Console.WriteLine("Done");