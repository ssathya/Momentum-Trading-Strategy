﻿// See https://aka.ms/new-console-template for more information
using SecurityPriceMaintain;

Console.WriteLine("Hello, World!");
var fh = new FunctionHandler();
await fh.DoApplicationProcessingAsync();
Console.WriteLine("Done");