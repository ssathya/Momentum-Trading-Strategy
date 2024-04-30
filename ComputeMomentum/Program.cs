using ComputeMomentum;
using Models;

Console.WriteLine("Computing values");
//create an instance of FunctionHandler and call DoApplicationProcessingAsync
var fh = new FunctionHandler();
List<SelectedTicker>? results = await fh.DoApplicationProcessingAsync();
if (results != null)
{
    Console.WriteLine("Ticker\tClosing Price\tYearly Gain\t6 months Gain\t4 Months Gain");
    DateTime markerDate = DateTime.Now;
    foreach (var result in results)
    {
        if (markerDate != result.Date)
        {
            Console.WriteLine("===============================");
        }
        markerDate = result.Date;
        Console.WriteLine($"{result.Ticker}\t{result.Date.ToShortDateString()}\t{result.Close.ToString("F2")}\t{result.AnnualPercentGain.ToString("F2")}\t{result.HalfYearlyPercentGain.ToString("F2")}\t{result.QuarterYearlyPercentGain.ToString("F2")}");
    }
}
Console.WriteLine("Done");