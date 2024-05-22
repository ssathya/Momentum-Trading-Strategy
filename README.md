# Momentum-Trading-Strategy
This is based on a strategy recommended by Portfolios for the investor: The best strategies to invest in stocks and ETFs while controlling risk by Luca Giusti. This book is written in Italian, and I don't have an English version of this book. The strategy goes as follows:
- Trading frequency monthly.
- Select the best 50 stocks of the NASDAQ-100 by percentage change for the last 12 months.
- From the above select the best 30 stocks by percentage change in the last 6 months.
- Of the 30 stocks pick the 10 best performing by percentage change in the last 3 months.


## Strategy
1. Trade frequency monthly
2. Select best 50 stocks by percentage change of the last 12 months
1. Select best 30 stocks by percentage change over the last 6 months
1. From the above 30 select top 10 stocks by percentage change in last 3 months
## Computation
### Monthly returns:
https://1drv.ms/x/s!ApMGIczTfCKAuYEPVJy3Gs1shFjQKg?e=YTgBQX

In an array of prices for a stock, to compute monthly return, take the price for a specific date and the price one month before the specific date. For example, on Thu, Dec 31, 2009 Apple was $6.415359 and on Fri, Jan 29, 2010 it was $5.846977. So, the monthly return for Apple as of 1/29/2010 would be as follows:
```
(5.846977/6.415359) -1 = -0.08859
```



### Function to compute for specific period
We will be using the same method to compute the return for 12 months, 6 months, and 3 months. 
```python
def get_rolling_ret(df, n):
  return df.rolling(n).apply(np.prod)
```


Equivalent C# code
```cs
using System;
using System.Collections.Generic;
using System.Linq;

public static class DataExtensions
{
public static IEnumerable<double?> GetRollingProduct(this IEnumerable<double?> source, int windowSize)
{
if (source == null) throw new ArgumentNullException(nameof(source));
if (windowSize <= 0) throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be greater than 0");

Queue<double?> queue = new Queue<double?>(windowSize);
foreach (var item in source)
{
if (queue.Count == windowSize)
{
queue.Dequeue();
}
queue.Enqueue(item);

if (queue.Count == windowSize)
{
yield return queue.Aggregate(1.0, (acc, x) => acc * x);
}
else
{
yield return null;
}
}
}
}

//usage
var data = new List<double?> { /* your data here */ };
var rollingProduct = data.GetRollingProduct(n);
```


### Rolling returns:
```python
ret_12,ret_6,ret_3 = get_rolling_ret(mtl,12),get_rolling_ret(mtl,6),get_rolling_ret(mtl,3)
```


### Cascading
To identify the top 50, we will apply the get_rolling_ret function to the previous year’s data. The outcomes will then be organized in descending order, from which we will extract the foremost 50.
Utilizing this curated list, we will invoke the get_rolling_ret function once more, this time with data spanning six months. We will arrange these results in descending order as well and isolate the leading 30.
For the final selection, we will proceed with the top 10. We will operate the get_rolling_ret function with a three-month data set, restricting our focus to only the tickers that were part of the top 30 previously identified.
#### Let us put words as code:
```python
def get_top(date):
  top_50 = ret_12.loc[date].nlargets(50).index
  top_30 = ret_6.loc[date].nlargets(30).index
  top_10 = ret_3.loc[date].nlargets(10).index
  return top_10
```  

#### Equivalent C# code
```cs
using System;
using System.Collections.Generic;
using System.Linq;

public class RollingReturnCalculator
{
// Assuming 'mtl' is a data structure that holds your market data.
private readonly MarketData mtl;

public RollingReturnCalculator(MarketData marketData)
{
mtl = marketData;
}

public IEnumerable<double?> GetRollingRet(int n)
{
// This method should implement the rolling product logic.
// It's a placeholder for the actual implementation.
return mtl.Rolling(n).Apply(np.Prod);
}

public IEnumerable<string> GetTop(DateTime date)
{
var ret12 = GetRollingRet(12);
var ret6 = GetRollingRet(6);
var ret3 = GetRollingRet(3);

var top50 = ret12.Loc(date).NLargest(50).Select(x => x.Index);
var top30 = ret6.Loc(date).NLargest(30).Select(x => x.Index);
var top10 = ret3.Loc(date).NLargest(10).Select(x => x.Index);

return top10;
}
}

// Usage:
// var calculator = new RollingReturnCalculator(yourMarketData);
// var top10OnDate = calculator.GetTop(yourDate);

```


Please note that this code is a direct translation and assumes the existence of certain methods (Rolling, Apply, Loc, NLargest) which are not standard in C#. You would need to implement these methods or use a library that provides similar functionality to pandas in Python. The MarketData class and np.Prod method are placeholders for your actual market data structure and the product calculation, respectively. The Index property is also assumed to be part of your data structure that holds the ticker symbol or identifier for the market data entry.
Remember to replace yourMarketData and yourDate with the actual data and date you’re working with. Also, you’ll need to define the MarketData class and its methods to match the functionality used in the Python code.