﻿// See https://aka.ms/new-console-template for more information
using SecurityPriceMaintain;

Console.WriteLine("Obtaining historic prices");
var fh = new FunctionHandler();
await fh.DoApplicationProcessingAsync();
Console.WriteLine("Done");