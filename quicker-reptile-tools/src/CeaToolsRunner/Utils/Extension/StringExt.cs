using System;
using System.Linq;
using System.Web;

namespace Cea.Utils.Extension
{
    public static class StringExt
    {
        /// <summary>
        /// Compare if string equals any of the given strings
        /// </summary>
        /// <param name="source">String to compare</param>
        /// <param name="ignoreCase">Indicates whether to ignore case when comparing</param>
        /// <param name="targets">Given list of strings</param>
        /// <returns>Returns true if string equals any of the given strings; otherwise false</returns>
        public static bool EqualsAny(this string source, bool ignoreCase, params string[] targets)
        {
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return targets.Any(t => string.Equals(source, t, comparison));
        }

        /// <summary>
        /// URL encode string
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <returns>Encoded string</returns>
        public static string UrlEncode(this string text) => HttpUtility.UrlEncode(text);

        /// <summary>
        /// Split string to list using specified separators
        /// </summary>
        /// <param name="str">String to split</param>
        /// <param name="splitter">Separator strings</param>
        /// <returns>Split string array</returns>
        public static string[] SplitToList(this string str, params string[] splitter)
        {
            if (str == null) return new string[0];
            return str.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Split string to list by line breaks
        /// </summary>
        /// <param name="str">String to split</param>
        /// <returns>Split string array</returns>
        public static string[] SplitToList(this string str) => str.SplitToList("\r\n", "\r", "\n");
    }
}

