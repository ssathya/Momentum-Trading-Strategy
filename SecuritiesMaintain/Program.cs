// See https://aka.ms/new-console-template for more information
using SecuritiesMaintain;

Console.WriteLine("Hello, World!");
var fh = new FunctionHandler();
var indexComponents = await fh.DoApplicationProcessingAsync();
Console.WriteLine($"Convering {indexComponents?.Count} firms");
Console.WriteLine("Done");