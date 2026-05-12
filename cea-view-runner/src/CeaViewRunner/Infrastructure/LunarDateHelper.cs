using System;
using System.Globalization;

namespace CeaViewRunner.Infrastructure;

/// <summary>
/// Minimal lunar date line for TimeWindow (replaces Cea.Utils.ChineseDateTime for standalone build).
/// </summary>
internal static class LunarDateHelper
{
    public static string GetDateString(DateTime solar)
    {
        try
        {
            var cal = new ChineseLunisolarCalendar();
            var year = cal.GetYear(solar);
            var month = cal.GetMonth(solar);
            var day = cal.GetDayOfMonth(solar);
            var leapMonth = cal.GetLeapMonth(year);
            var isLeap = leapMonth > 0 && month == leapMonth + 1;
            if (leapMonth > 0 && month > leapMonth)
            {
                month--;
            }

            return $"农历{(isLeap ? "闰" : string.Empty)}{month}月{day}日";
        }
        catch
        {
            return string.Empty;
        }
    }
}
