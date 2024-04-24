namespace ComputeMomentum.Internal;

internal class DataFrameSim
{
    public string Ticker { get; set; } = string.Empty;
    public Dictionary<DateOnly, double> ValueByDate = [];
}