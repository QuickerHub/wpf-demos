using System;

namespace QuickerActionManage.Utils.Extension
{
    /// <summary>
    /// DateTime extension methods
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Convert UTC time to local time
        /// </summary>
        public static DateTime UtcToLocalTime(this DateTime utcTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), TimeZoneInfo.Local);
        }
    }
}

