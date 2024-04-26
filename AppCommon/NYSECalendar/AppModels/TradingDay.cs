namespace AppCommon.NYSECalendar.AppModels;
public class TradingDay
{
    public DateTime Date { get; init; }
    public bool BusinessDay { get; init; }
    public bool PublicHoliday { get; init; }
    public bool Weekend { get; init; }
}
