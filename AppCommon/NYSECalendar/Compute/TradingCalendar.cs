using AppCommon.NYSECalendar.AppModels;
using System.Runtime.InteropServices;

namespace AppCommon.NYSECalendar.Compute;

public static class TradingCalendar
{
    public static void ConvertTimeToEST(out DateTime dateInEst, DateTime currentTime = default)
    {
        currentTime = currentTime == default ? DateTime.UtcNow : currentTime;
        TimeZoneInfo est;
        est = BuildTimeZoneInfo();

        dateInEst = TimeZoneInfo.ConvertTime(currentTime, est);
    }

    public static DateTime FirstTradingDayOfMonth(int month = 0, int year = 0)
    {
        IEnumerable<TradingDay> tradingDays = TradingDaysOfMonth(ref month, ref year);
        return tradingDays.First().Date;
    }

    public static bool FirstTradingDayOfWeek(DateTime? date)
    {
        GetWeekTradingDays(date, out DateTime dateToUse, out DateTime firstDayOfWeek, out DateTime lastDayOfWeek);
        var tradingDays = GetTradingDays(firstDayOfWeek, lastDayOfWeek);
        return tradingDays.First().Date.Date == dateToUse.Date;
    }

    public static TradingDay GetTradingDay()
    {
        var today = DateTime.Now.Date;
        return GetTradingDay(today);
    }

    public static TradingDay GetTradingDay(DateTime day)
    {
        var todayResult = GetTradingDays(day, day);
        var yesterdayResult = GetTradingDays(day.AddDays(-1), day.AddDays(-1));
        if (todayResult == null || !todayResult.Any())
        {
            return new TradingDay
            {
                BusinessDay = false,
                Date = day,
                PublicHoliday = true,
                Weekend = day.DayOfWeek == DayOfWeek.Sunday || day.DayOfWeek == DayOfWeek.Saturday
            };
        }
        return GetTradingDays(day, day).First();
    }

    public static IEnumerable<TradingDay> GetTradingDays(DateTime day1, DateTime day2)
    {
        var startDate = (day1 <= day2 ? day1 : day2).Date;
        var endDate = (day2 > day1 ? day2 : day1).Date;
        if (endDate.Subtract(startDate).TotalDays > 366)
        {
            throw new ArgumentOutOfRangeException("Time span more than a year");
        }

        var nonWorkingDaysH = ExchangeCalendar.GetAllNonWorkingDays(startDate.Year);
        if (endDate.Year != startDate.Year)
        {
            nonWorkingDaysH.AddRange(ExchangeCalendar.GetAllNonWorkingDays(endDate.Year));
        }
        nonWorkingDaysH = (from nwd in nonWorkingDaysH where nwd.Date >= startDate && nwd.Date <= endDate select nwd).ToList(
            );
        var nonWorkingDays = nonWorkingDaysH.Select(nwd => nwd.Date);
        var allDaysInSpan = ExchangeCalendar.GetDaysBetween(startDate, endDate);
        allDaysInSpan = allDaysInSpan.Except(nonWorkingDays);
        return from d in allDaysInSpan select new TradingDay { BusinessDay = true, Date = d };
    }

    public static DateTime LastTradingDayOfMonth(int month = 0, int year = 0)
    {
        IEnumerable<TradingDay> tradingDays = TradingDaysOfMonth(ref month, ref year);
        return tradingDays.Last().Date;
    }

    public static bool LastTradingDayOfWeek(DateTime? date)
    {
        GetWeekTradingDays(date, out DateTime dateToUse, out DateTime firstDayOfWeek, out DateTime lastDayOfWeek);
        var tradingDays = GetTradingDays(firstDayOfWeek, lastDayOfWeek);
        return tradingDays.Last().Date.Date == dateToUse.Date;
    }

    public static DateTime NextTradeTime(DateTime? startTime)
    {
        DateTime timeToEval = startTime ?? DateTime.UtcNow;
        if (IsMarketOpen(timeToEval))
        {
            return timeToEval;
        }
        ConvertTimeToEST(out DateTime dateInEst, timeToEval);
        DateTime tradeStratTime = new(dateInEst.Year, dateInEst.Month, dateInEst.Day, 9, 30, 0);
        TimeZoneInfo est = BuildTimeZoneInfo();
        DateTime tradeTimeWithOffset = (new DateTimeOffset(tradeStratTime, est.BaseUtcOffset)).DateTime;
        bool includeStartTime = false;
        if (dateInEst < tradeTimeWithOffset)
        {
            includeStartTime = true;
        }
        var tradingDates = GetTradingDays(timeToEval, timeToEval.AddDays(5));
        TradingDay nextTradingDate = includeStartTime ? tradingDates.First() : tradingDates.Skip(1).First();
        DateTime nxtTradingDt = nextTradingDate.Date.ToLocalTime();
        nxtTradingDt = nxtTradingDt.AddHours(9).AddMinutes(30);
        return nxtTradingDt;
    }

    public static IEnumerable<TradingDay> TradingDaysOfMonth(ref int month, ref int year)
    {
        if (month == 0)
        {
            month = DateTime.Now.Month;
        }
        if (year == 0)
        {
            year = DateTime.Now.Year;
        }
        var firstDayOfMonth = new DateTime(year, month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        var tradingDays = GetTradingDays(firstDayOfMonth, lastDayOfMonth);
        return tradingDays;
    }

    private static TimeZoneInfo BuildTimeZoneInfo()
    {
        TimeZoneInfo est;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            est = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        else
        {
            est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }

        return est;
    }

    private static void GetWeekTradingDays(
        DateTime? date,
        out DateTime dateToUse,
        out DateTime firstDayOfWeek,
        out DateTime lastDayOfWeek)
    {
        if (date == null)
        {
            dateToUse = DateTime.Now;
        }
        else
        {
            dateToUse = (DateTime)date;
        }

        firstDayOfWeek = dateToUse;
        while (firstDayOfWeek.DayOfWeek != DayOfWeek.Monday)
        {
            firstDayOfWeek = firstDayOfWeek.AddDays(-1);
        }
        lastDayOfWeek = dateToUse;
        while (lastDayOfWeek.DayOfWeek != DayOfWeek.Friday)
        {
            lastDayOfWeek = lastDayOfWeek.AddDays(1);
        }
    }

    private static bool IsMarketOpen(DateTime date)
    {
        ConvertTimeToEST(out DateTime dateInEst, date);
        TimeOnly currentTime = new(dateInEst.Hour, dateInEst.Minute, dateInEst.Second);
        TimeOnly tradeStartTime = new(09, 30);
        TimeOnly tradeEndTime = new(16, 30);
        return (GetTradingDay(dateInEst).BusinessDay == true &&
            currentTime >= tradeStartTime &&
            currentTime <= tradeEndTime);
    }
}