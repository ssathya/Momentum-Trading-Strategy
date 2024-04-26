using AppCommon.NYSECalendar.AppModels;

namespace AppCommon.NYSECalendar.Compute;

public static class ExchangeCalendar
{
    /* Rule 1: When any stock market holiday falls on a Saturday, the market will be closed on the previous day (Friday)
     *         unless the Friday is the end of a monthly or yearly accounting period.
     *
     * Rule 2: When any stock market holiday falls on a Sunday, the market will be closed the next day (Monday).
     *
     * Rule 3: Special Holidays
     *         Martin Luther King, Jr. Day is always observed on the third Monday in January.
     *         President’s Day is always observed on the third Monday in February.
     *         Memorial Day is always observed on the last Monday in May
     *         Good Friday, the Friday before easter sunday
     *         Labor Day, the first Monday of September
     *         Thanksgiving Day, the fourth Thursday of November
     */

    #region Public Methods

    public static List<NonWorkingDay> GetAllNonWorkingDays(int year)
    {
        var firstDayOfYear = new DateTime(year, 1, 1);
        var allNonWorkingDays = (from weekend in GetDaysBetween(firstDayOfYear, firstDayOfYear.AddDays(365))
            .Where(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            .Where(d => d.Year == year)
                                 select new NonWorkingDay { Date = weekend, Holidays = Holidays.Weekend }).ToList();
        var holidays = GetHolidaysByYear(year);
        allNonWorkingDays.AddRange(from holiday in holidays
                                   select new NonWorkingDay { Date = holiday.Value, Holidays = holiday.Key });
        allNonWorkingDays = allNonWorkingDays.OrderBy(a => a.Date).ToList();
        return allNonWorkingDays;
    }

    public static Dictionary<Holidays, DateTime> GetHolidaysByYear(int year)
    {
        var holidays = new Dictionary<Holidays, DateTime>();

        var date = new DateTime(year, 1, 1);
        //If date falls on Saturday, no holiday is set since Friday is last day of accounting year.
        if (date.DayOfWeek != DayOfWeek.Saturday)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday)
                date = date.AddDays(1);
            holidays.Add(Holidays.NewYear, date);
        }

        holidays.Add(Holidays.MLK, GetMartinLutherKingDay(year));
        holidays.Add(Holidays.Presidents, GetPresidentsDay(year));
        holidays.Add(Holidays.GoodFriday, GetGoodFriday(year));
        holidays.Add(Holidays.Memorial, GetMemorialDay(year));
        holidays.Add(Holidays.Juneteenth, AdjustDate(new DateTime(year, 6, 19)));
        holidays.Add(Holidays.Independence, AdjustDate(new DateTime(year, 7, 4)));
        holidays.Add(Holidays.Labor, GetLaborDay(year));
        holidays.Add(Holidays.Thanksgiving, GetThanksgivingDay(year));
        holidays.Add(Holidays.Christmas, AdjustDate(new DateTime(year, 12, 25)));
        return holidays;
    }

    #endregion Public Methods

    #region Internal Methods

    internal static DateTime AdjustDate(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday)
            return date.AddDays(-1);
        if (date.DayOfWeek == DayOfWeek.Sunday)
            return date.AddDays(1);
        return date;
    }

    internal static IEnumerable<DateTime> GetDaysBetween(DateTime start, DateTime end)
    {
        for (var i = start; i <= end; i = i.AddDays(1))
        {
            yield return i;
        }
    }

    internal static DateTime GetEasterSunday(int year)
    {
        int y = year;
        int a = y % 19;
        int b = y / 100;
        int c = y % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(year, month, day);
    }

    internal static DateTime GetGoodFriday(int year)
    {
        return GetEasterSunday(year).AddDays(-2);
    }

    internal static DateTime GetLaborDay(int year)
    {
        var date = new DateTime(year, 9, 1);
        return GetMonday(date);
    }

    internal static DateTime GetMartinLutherKingDay(int year)
    {
        var date = new DateTime(year, 1, 1);
        date = GetMonday(date);
        return date.AddDays(14);
    }

    internal static DateTime GetMemorialDay(int year)
    {
        var date = new DateTime(year, 5, 1);
        date = GetMonday(date);
        var assumedLast = date.AddDays(28);
        return assumedLast.Month == date.Month ? assumedLast : date.AddDays(21);
    }

    internal static DateTime GetMonday(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Monday)
            return date;

        if (date.DayOfWeek == DayOfWeek.Sunday)
            return date.AddDays(1);

        var offset = 8 - (int)date.DayOfWeek;
        return date.AddDays(offset);
    }

    internal static DateTime GetPresidentsDay(int year)
    {
        var date = new DateTime(year, 2, 1);
        date = GetMonday(date);
        return date.AddDays(14);
    }

    internal static DateTime GetThanksgivingDay(int year)
    {
        var date = new DateTime(year, 11, 1);
        date = GetThursday(date);
        return date.AddDays(21);
    }

    internal static DateTime GetThursday(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Friday)
            return date.AddDays(6);

        if (date.DayOfWeek == DayOfWeek.Saturday)
            return date.AddDays(5);

        var offset = 4 - (int)date.DayOfWeek;
        return date.AddDays(offset);
    }

    internal static bool IsHoliday(DateTime date)
    {
        var holidays = GetHolidaysByYear(date.Year);
        return holidays.Any(x => x.Value.Year == date.Year &&
           x.Value.Month == date.Month && x.Value.Day == date.Day);
    }

    #endregion Internal Methods
}