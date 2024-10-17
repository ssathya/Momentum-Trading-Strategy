// See https://aka.ms/new-console-template for more information
using ComputeSlopeSummary;

Console.WriteLine("Computing slope summaries!");
var fh = new FunctionHandler();
await fh.DoApplicationProcessingAsync();
Console.WriteLine("Done");