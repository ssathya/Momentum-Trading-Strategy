using ComputeMomentum;

Console.WriteLine("Computing values");
//create an instance of FunctionHandler and call DoApplicationProcessingAsync
var fh = new FunctionHandler();
var results = await fh.DoApplicationProcessingAsync();
if (results != null)
{
    DateTime markerDate = DateTime.Now;
    foreach (var result in results)
    {
        if (markerDate != result.Date)
        {
            Console.WriteLine("===============================");
        }
        markerDate = result.Date;
        Console.WriteLine($"{result.Ticker}\t{result.Date.ToShortDateString()}\t{result.Close}");
    }
}
Console.WriteLine("Done");