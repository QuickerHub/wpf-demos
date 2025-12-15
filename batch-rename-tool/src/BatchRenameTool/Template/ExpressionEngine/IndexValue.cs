using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using BatchRenameTool.Template.Utils;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents an index value with expression evaluation support
    /// Supports expressions like {2i+1:00} and formatting
    /// </summary>
    public class IndexValue : NumberValue
    {
        private readonly int _totalCount;

        public IndexValue(int index, int totalCount) : base(index)
        {
            _totalCount = totalCount;
        }

        /// <summary>
        /// Evaluate an expression with this index value
        /// Example: "2i+1" with index=0 -> 1
        /// </summary>
        public ITemplateValue EvaluateExpression(string expression, string? formatString = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return FormatIndex(GetValue() as int? ?? 0, formatString);
            }

            try
            {
                // Remove whitespace
                var normalized = expression.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

                if (string.IsNullOrEmpty(normalized))
                {
                    return FormatIndex(GetValue() as int? ?? 0, formatString);
                }

                // Normalize expression: replace implicit multiplication (e.g., "2i" -> "2*i")
                normalized = Regex.Replace(normalized, @"(\d+)([iI])", "$1*$2", RegexOptions.IgnoreCase);
                normalized = Regex.Replace(normalized, @"([iI])(\d+)", "$1*$2", RegexOptions.IgnoreCase);

                // Replace 'i' with the actual index value
                normalized = Regex.Replace(normalized, @"\b[iI]\b", (GetValue() as int? ?? 0).ToString(), RegexOptions.IgnoreCase);

                // Validate expression contains only numbers, operators, and parentheses
                if (!Regex.IsMatch(normalized, @"^[0-9+\-*/().\s]+$"))
                {
                    return FormatIndex(GetValue() as int? ?? 0, formatString);
                }

                // Use DataTable to evaluate the expression safely
                var dataTable = new DataTable();
                var result = dataTable.Compute(normalized, null);

                // Convert to integer
                int evaluatedIndex = Convert.ToInt32(Math.Round(Convert.ToDouble(result)));

                return FormatIndex(evaluatedIndex, formatString);
            }
            catch
            {
                return FormatIndex(GetValue() as int? ?? 0, formatString);
            }
        }

        private ITemplateValue FormatIndex(int index, string? formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                return new StringValue(index.ToString());
            }

            // Check if it's a Chinese number format
            if (ChineseNumberConverter.IsChineseNumber(formatString[0]))
            {
                return new StringValue(FormatChineseIndex(index, formatString));
            }

            // Numeric format
            return new StringValue(FormatNumericIndex(index, formatString));
        }

        private string FormatNumericIndex(int index, string formatString)
        {
            int offset = 0;
            int padding = 0;

            if (formatString.Length > 0 && char.IsDigit(formatString[0]))
            {
                int firstNonZeroIndex = -1;
                for (int i = 0; i < formatString.Length; i++)
                {
                    if (formatString[i] != '0')
                    {
                        firstNonZeroIndex = i;
                        break;
                    }
                }

                if (firstNonZeroIndex >= 0)
                {
                    offset = formatString[firstNonZeroIndex] - '0';
                    padding = formatString.Length;
                }
                else
                {
                    // All zeros, no offset, just padding
                    offset = 0;
                    padding = formatString.Length;
                }
            }

            int value = index + offset;

            // Ensure value is non-negative for padding
            if (value < 0)
            {
                value = 0;
            }

            if (padding > 0)
            {
                // Use ToString with padding format, but ensure it doesn't cause issues
                try
                {
                    return value.ToString($"D{padding}");
                }
                catch
                {
                    // Fallback if format fails
                    return value.ToString().PadLeft(padding, '0');
                }
            }

            return value.ToString();
        }

        private string FormatChineseIndex(int index, string formatString)
        {
            bool useUpper = ChineseNumberConverter.IsUpperChineseNumber(formatString[0]);
            int startValue = 0;

            var firstChar = formatString[0].ToString();
            var lowerDigits = new[] { "?", "?", "?", "?", "?", "?", "?", "?", "?", "?" };
            var upperDigits = new[] { "?", "?", "?", "?", "?", "?", "?", "?", "?", "?" };

            var digits = useUpper ? upperDigits : lowerDigits;
            for (int i = 0; i < digits.Length; i++)
            {
                if (digits[i] == firstChar)
                {
                    startValue = i;
                    break;
                }
            }

            if (formatString[0] == '?' || formatString[0] == '?')
            {
                startValue = 10;
            }

            int value = index + startValue;
            return ChineseNumberConverter.ToChinese(value, useUpper);
        }

        public new string ToString(string? format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                return (GetValue() as int? ?? 0).ToString();
            }

            return FormatIndex(GetValue() as int? ?? 0, format).ToString();
        }
    }
}

