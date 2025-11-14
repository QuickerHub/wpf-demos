using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Text utility methods
    /// </summary>
    public static class TextUtil
    {
        /// <summary>
        /// Find count of search string in main string
        /// </summary>
        public static int FindCount(string? mainString, string searchString)
        {
            if (mainString == null)
                return 0;

            int count = 0;
            int index = 0;

            while ((index = mainString.IndexOf(searchString, index)) != -1)
            {
                count++;
                index += searchString.Length;
            }

            return count;
        }

        /// <summary>
        /// Search text in multiple strings with pinyin support
        /// </summary>
        public static bool Search(string? search, params string?[] textList)
        {
            if (search == null || string.IsNullOrWhiteSpace(search))
            {
                return true;
            }
            else
            {
                var keys = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(key => key.Replace("'", ""));

                return textList.Any(x => keys.All(k => x?.IsPinyinMatchOrContains(k) ?? false));
            }
        }

        private static bool IsPinyinMatchOrContains(this string text, string search)
        {
            return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1
                || PinyinHelper.IsPinyinMatch(PinyinHelper.GetPinYinMatchString(text), search, false, false);
        }

        /// <summary>
        /// Get relative time string
        /// </summary>
        public static string GetRelativeTimeString(DateTime time, DateTime? target = null)
        {
            var time0 = target ?? DateTime.Now;
            var date = time0.Date;

            if (time >= date)
            {
                var sub = time0 - time;
                if (sub.TotalSeconds < 60)
                    return "刚刚";
                if (sub.TotalMinutes < 60)
                    return $"{sub.Minutes}分钟前";
                return $"{(int)sub.TotalHours}小时前";
            }

            if (time >= date - TimeSpan.FromDays(1))
                return time.ToString("昨天 HH:mm");

            if (time >= date - TimeSpan.FromDays(3))
                return $"{(time0 - time).Days}天前";
            if (time.Year == date.Year)
                return time.ToString("MM月 dd日");
            return time.ToString("yyyy.MM.dd");
        }
    }
}

