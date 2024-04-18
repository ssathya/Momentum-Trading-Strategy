// See https://aka.ms/new-console-template for more information
using ComputeMomentum;

Console.WriteLine("Computing values");
//create an instance of FunctionHandler and call DoApplicationProcessingAsync
var fh = new FunctionHandler();
await fh.DoApplicationProcessingAsync();
Console.WriteLine("Done");