namespace AppCommon;

public static class AppExtensions
{
    /// <summary>
    /// Computes the Median of a given list of integers
    /// </summary>
    /// <param name="list"></param>
    /// <returns>The mean value</returns>
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static double Median(this List<int> list)
    {
        list.Sort();
        int count = list.Count();
        return list.Skip((count - 1) / 2).Take(2 - count % 2).Average();
    }
}