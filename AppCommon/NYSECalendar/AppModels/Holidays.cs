using System.ComponentModel;

namespace AppCommon.NYSECalendar.AppModels;

public enum Holidays
{
    [Description("New Year's Day")]
    NewYear,

    [Description("Martin Luther King, Jr. Day")]
    MLK,

    [Description("Presidents' Day")]
    Presidents,

    [Description("Good Friday")]
    GoodFriday,

    [Description("Memorial Day")]
    Memorial,

    [Description("Juneteenth Day")]
    Juneteenth,

    [Description("Independence Day")]
    Independence,

    [Description("Labor Day")]
    Labor,

    [Description("Thanksgiving Day")]
    Thanksgiving,

    [Description("Christmas Day")]
    Christmas,

    [Description("Weekend")]
    Weekend
}