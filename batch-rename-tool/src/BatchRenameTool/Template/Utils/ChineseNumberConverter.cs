using System;
using System.Text;

namespace BatchRenameTool.Template.Utils
{
    /// <summary>
    /// Convert numbers to Chinese number strings
    /// </summary>
    public static class ChineseNumberConverter
    {
        private static readonly string[] LowerDigits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        private static readonly string[] UpperDigits = { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };
        private static readonly string[] LowerUnits = { "", "十", "百", "千", "万" };
        private static readonly string[] UpperUnits = { "", "拾", "佰", "仟", "万" };

        /// <summary>
        /// Convert number to Chinese string
        /// </summary>
        /// <param name="number">Number to convert</param>
        /// <param name="useUpper">Use upper case Chinese numbers (壹, 贰, etc.)</param>
        /// <returns>Chinese number string</returns>
        public static string ToChinese(int number, bool useUpper = false)
        {
            if (number == 0)
                return useUpper ? UpperDigits[0] : LowerDigits[0];

            var digits = useUpper ? UpperDigits : LowerDigits;
            var units = useUpper ? UpperUnits : LowerUnits;
            var sb = new StringBuilder();

            if (number < 0)
            {
                sb.Append("负");
                number = -number;
            }

            // Handle numbers less than 10
            if (number < 10)
            {
                return digits[number];
            }

            // Handle numbers 10-99
            if (number < 100)
            {
                int tens = number / 10;
                int ones = number % 10;

                if (tens == 1)
                {
                    sb.Append(units[1]); // "十"
                }
                else
                {
                    sb.Append(digits[tens]);
                    sb.Append(units[1]); // "十"
                }

                if (ones > 0)
                {
                    sb.Append(digits[ones]);
                }

                return sb.ToString();
            }

            // Handle larger numbers (simplified, up to 9999)
            if (number < 10000)
            {
                int thousands = number / 1000;
                int hundreds = (number % 1000) / 100;
                int tens = (number % 100) / 10;
                int ones = number % 10;

                if (thousands > 0)
                {
                    sb.Append(digits[thousands]);
                    sb.Append(units[3]); // "千"
                }

                if (hundreds > 0)
                {
                    sb.Append(digits[hundreds]);
                    sb.Append(units[2]); // "百"
                }
                else if (thousands > 0 && (tens > 0 || ones > 0))
                {
                    sb.Append(digits[0]); // "零"
                }

                if (tens > 0)
                {
                    if (tens == 1 && thousands == 0 && hundreds == 0)
                    {
                        sb.Append(units[1]); // "十"
                    }
                    else
                    {
                        sb.Append(digits[tens]);
                        sb.Append(units[1]); // "十"
                    }
                }
                else if ((thousands > 0 || hundreds > 0) && ones > 0)
                {
                    sb.Append(digits[0]); // "零"
                }

                if (ones > 0)
                {
                    sb.Append(digits[ones]);
                }

                return sb.ToString();
            }

            // For numbers >= 10000, return as-is or use a more complex conversion
            // For simplicity, we'll convert digit by digit
            return ConvertLargeNumber(number, digits, units);
        }

        private static string ConvertLargeNumber(int number, string[] digits, string[] units)
        {
            var sb = new StringBuilder();
            var numStr = number.ToString();
            var len = numStr.Length;

            for (int i = 0; i < len; i++)
            {
                int digit = numStr[i] - '0';
                int pos = len - i - 1;

                if (digit != 0)
                {
                    sb.Append(digits[digit]);
                    if (pos > 0 && pos < units.Length)
                    {
                        sb.Append(units[pos]);
                    }
                }
                else if (i < len - 1 && numStr[i + 1] != '0')
                {
                    sb.Append(digits[0]); // "零"
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Detect if a character is a Chinese number (lower or upper case)
        /// </summary>
        public static bool IsChineseNumber(char ch)
        {
            var chStr = ch.ToString();
            return Array.IndexOf(LowerDigits, chStr) >= 0 ||
                   Array.IndexOf(UpperDigits, chStr) >= 0 ||
                   ch == '十' || ch == '拾';
        }

        /// <summary>
        /// Detect if a character is an upper case Chinese number
        /// </summary>
        public static bool IsUpperChineseNumber(char ch)
        {
            var chStr = ch.ToString();
            return Array.IndexOf(UpperDigits, chStr) >= 0 || ch == '拾';
        }
    }
}
