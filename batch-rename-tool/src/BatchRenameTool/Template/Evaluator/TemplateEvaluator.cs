using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Utils;

namespace BatchRenameTool.Template.Evaluator
{
    /// <summary>
    /// Context for template evaluation
    /// </summary>
    public class EvaluationContext
    {
        public string Name { get; set; } = "";           // File name without extension
        public string Ext { get; set; } = "";             // Extension without dot
        public string FullName { get; set; } = "";       // Full file name
        public int Index { get; set; } = 0;              // Index for {i} variable
        public int TotalCount { get; set; } = 0;         // Total count for {iv} variable (reverse index)
        public DateTime Today { get; set; } = DateTime.Today;  // Current date for {today} variable
        public DateTime Now { get; set; } = DateTime.Now;     // Current date/time for {now} variable
    }

    /// <summary>
    /// Evaluator for template AST
    /// </summary>
    public class TemplateEvaluator
    {
        /// <summary>
        /// Evaluate template node with context
        /// </summary>
        public string Evaluate(TemplateNode node, EvaluationContext context)
        {
            var sb = new StringBuilder();

            foreach (var astNode in node.Nodes)
            {
                sb.Append(EvaluateNode(astNode, context));
            }

            return sb.ToString();
        }

        private string EvaluateNode(AstNode node, EvaluationContext context)
        {
            return node switch
            {
                TextNode textNode => textNode.Text,
                VariableNode varNode => EvaluateVariable(varNode, context),
                FormatNode formatNode => EvaluateFormat(formatNode, context),
                MethodNode methodNode => EvaluateMethod(methodNode, context),
                SliceNode sliceNode => EvaluateSlice(sliceNode, context),
                _ => $"[未支持的节点类型: {node.GetType().Name}]"
            };
        }

        private string EvaluateVariable(VariableNode node, EvaluationContext context)
        {
            return node.VariableName.ToLower() switch
            {
                "name" => context.Name,
                "ext" => context.Ext,
                "fullname" => context.FullName,
                "i" => context.Index.ToString(),
                "iv" => (context.TotalCount - 1 - context.Index).ToString(), // Reverse index
                "today" => context.Today.ToString("yyyy-MM-dd"), // Default format
                "now" => context.Now.ToString("yyyy-MM-dd HH:mm:ss"), // Default format
                _ => $"{{{node.VariableName}}}"
            };
        }

        private string EvaluateFormat(FormatNode node, EvaluationContext context)
        {
            // Support both {variable:format} and {expression:format} syntax
            // If expression is provided, evaluate it first (for {2i+1:00} syntax)
            if (!string.IsNullOrEmpty(node.ExpressionString))
            {
                int index = EvaluateExpression(node.ExpressionString, context.Index);
                return FormatIndex(index, node.FormatString);
            }
            
            // Otherwise, check the inner node type
            if (node.InnerNode is VariableNode varNode)
            {
                var varName = varNode.VariableName.ToLower();
                
                // Handle number variable (i)
                if (varName == "i")
                {
                    return FormatIndex(context.Index, node.FormatString);
                }
                
                // Handle reverse index variable (iv)
                if (varName == "iv")
                {
                    int reverseIndex = context.TotalCount > 0 ? context.TotalCount - 1 - context.Index : 0;
                    return FormatIndex(reverseIndex, node.FormatString);
                }
                
                // Handle date variable (today)
                if (varName == "today")
                {
                    return FormatDate(context.Today, node.FormatString);
                }
                
                // Handle datetime variable (now)
                if (varName == "now")
                {
                    return FormatDateTime(context.Now, node.FormatString);
                }
            }
            
            // For other variables, format is not supported
            return EvaluateNode(node.InnerNode, context);
        }

        private string FormatIndex(int index, string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                return index.ToString();
            }

            // Check if it's a Chinese number format
            if (ChineseNumberConverter.IsChineseNumber(formatString[0]))
            {
                return FormatChineseIndex(index, formatString);
            }

            // Numeric format
            return FormatNumericIndex(index, formatString);
        }

        private string FormatNumericIndex(int index, string formatString)
        {
            // Parse format string to determine offset and padding
            // Examples: 
            // - "001" -> offset = 1, format = "000" (equivalent to {i+1:000})
            // - "01" -> offset = 1, format = "00" (equivalent to {i+1:00})
            // - "1" -> offset = 1, format = "" (equivalent to {i+1})
            // - "000" -> offset = 0, format = "000" (equivalent to {i:000})
            // - "00" -> offset = 0, format = "00" (equivalent to {i:00})

            int offset = 0;
            int padding = 0;

            if (formatString.Length > 0 && char.IsDigit(formatString[0]))
            {
                // Find the first non-zero digit
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
                    // Found non-zero digit: extract offset and determine padding
                    // Offset is the value of the first non-zero digit
                    offset = formatString[firstNonZeroIndex] - '0';
                    // Padding is determined by the total length of the format string
                    padding = formatString.Length;
                }
                else
                {
                    // All zeros: offset = 0, padding = length
                    offset = 0;
                    padding = formatString.Length;
                }
            }

            int value = index + offset;
            
            // Apply padding if format string has multiple digits
            if (padding > 0)
            {
                return value.ToString($"D{padding}");
            }
            
            return value.ToString();
        }

        private string FormatChineseIndex(int index, string formatString)
        {
            // Check if upper case Chinese numbers are used
            bool useUpper = ChineseNumberConverter.IsUpperChineseNumber(formatString[0]);

            // Determine start value from first character
            int startValue = 0;

            // Map Chinese number to start value
            var firstChar = formatString[0].ToString();
            var lowerDigits = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            var upperDigits = new[] { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };

            var digits = useUpper ? upperDigits : lowerDigits;
            for (int i = 0; i < digits.Length; i++)
            {
                if (digits[i] == firstChar)
                {
                    startValue = i;
                    break;
                }
            }

            // Special case: "十" or "拾" means start from 10
            if (formatString[0] == '十' || formatString[0] == '拾')
            {
                startValue = 10;
            }

            int value = index + startValue;
            return ChineseNumberConverter.ToChinese(value, useUpper);
        }

        /// <summary>
        /// Format date using format string
        /// </summary>
        private string FormatDate(DateTime date, string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                return date.ToString("yyyy-MM-dd");
            }

            try
            {
                return date.ToString(formatString);
            }
            catch
            {
                // If format string is invalid, return default format
                return date.ToString("yyyy-MM-dd");
            }
        }

        /// <summary>
        /// Format date/time using format string
        /// </summary>
        private string FormatDateTime(DateTime dateTime, string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            try
            {
                return dateTime.ToString(formatString);
            }
            catch
            {
                // If format string is invalid, return default format
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        private string EvaluateMethod(MethodNode node, EvaluationContext context)
        {
            // Evaluate target first
            var targetValue = EvaluateNode(node.Target, context);
            
            // Ensure targetValue is a string
            if (targetValue == null)
            {
                targetValue = "";
            }
            
            // Evaluate arguments
            var arguments = new List<object>();
            foreach (var arg in node.Arguments)
            {
                if (arg is LiteralNode literalNode)
                {
                    arguments.Add(literalNode.Value);
                }
                else
                {
                    // Evaluate complex argument expressions
                    var argValue = EvaluateNode(arg, context);
                    arguments.Add(argValue);
                }
            }
            
            // Get method name (case-insensitive)
            var methodName = node.MethodName.ToLower();
            
            // Handle method aliases
            var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "替换", "replace" },
                { "大写", "upper" },
                { "小写", "lower" },
                { "去空格", "trim" },
                { "截取", "sub" },
                { "切片", "sub" },
                { "左填充", "padleft" },
                { "右填充", "padright" }
            };
            
            if (aliasMap.ContainsKey(methodName))
            {
                methodName = aliasMap[methodName];
            }
            
            // Execute method based on name
            return methodName switch
            {
                "replace" => ExecuteReplace(targetValue, arguments),
                "upper" => ExecuteUpper(targetValue),
                "lower" => ExecuteLower(targetValue),
                "trim" => ExecuteTrim(targetValue),
                "sub" => ExecuteSub(targetValue, arguments),
                "padleft" => ExecutePadLeft(targetValue, arguments),
                "padright" => ExecutePadRight(targetValue, arguments),
                _ => $"[未知方法: {node.MethodName}]"
            };
        }

        private string ExecuteReplace(string target, List<object> arguments)
        {
            if (arguments.Count < 2)
            {
                return target; // Not enough arguments, return original
            }
            
            var oldValue = arguments[0]?.ToString() ?? "";
            var newValue = arguments[1]?.ToString() ?? "";
            
            return target.Replace(oldValue, newValue);
        }

        private string ExecuteUpper(string target)
        {
            return target.ToUpper();
        }

        private string ExecuteLower(string target)
        {
            return target.ToLower();
        }

        private string ExecuteTrim(string target)
        {
            return target.Trim();
        }

        private string ExecuteSub(string target, List<object> arguments)
        {
            if (arguments.Count == 0)
            {
                return target; // No arguments, return original
            }
            
            // Get start index
            int start = 0;
            if (arguments[0] is int startInt)
            {
                start = startInt;
            }
            else if (int.TryParse(arguments[0]?.ToString(), out int parsedStart))
            {
                start = parsedStart;
            }
            
            // Handle negative indices (Python-style)
            if (start < 0)
            {
                start = target.Length + start;
            }
            
            // Clamp start to valid range
            start = Math.Max(0, Math.Min(start, target.Length));
            
            // Get end index (optional)
            if (arguments.Count >= 2)
            {
                int end = target.Length;
                if (arguments[1] is int endInt)
                {
                    end = endInt;
                }
                else if (int.TryParse(arguments[1]?.ToString(), out int parsedEnd))
                {
                    end = parsedEnd;
                }
                
                // Handle negative indices
                if (end < 0)
                {
                    end = target.Length + end;
                }
                
                // Clamp end to valid range
                end = Math.Max(0, Math.Min(end, target.Length));
                
                // Extract substring
                if (start >= end)
                {
                    return ""; // Invalid range
                }
                
                return target.Substring(start, end - start);
            }
            else
            {
                // Only start index provided, return from start to end
                return target.Substring(start);
            }
        }

        private string ExecutePadLeft(string target, List<object> arguments)
        {
            if (arguments.Count == 0)
            {
                return target; // No arguments, return original
            }
            
            // Get total width
            int totalWidth = target.Length;
            if (arguments[0] is int widthInt)
            {
                totalWidth = widthInt;
            }
            else if (int.TryParse(arguments[0]?.ToString(), out int parsedWidth))
            {
                totalWidth = parsedWidth;
            }
            
            // Get padding character (optional, default is space)
            char paddingChar = ' ';
            if (arguments.Count >= 2)
            {
                var paddingStr = arguments[1]?.ToString() ?? " ";
                if (paddingStr.Length > 0)
                {
                    paddingChar = paddingStr[0];
                }
            }
            
            return target.PadLeft(totalWidth, paddingChar);
        }

        private string ExecutePadRight(string target, List<object> arguments)
        {
            if (arguments.Count == 0)
            {
                return target; // No arguments, return original
            }
            
            // Get total width
            int totalWidth = target.Length;
            if (arguments[0] is int widthInt)
            {
                totalWidth = widthInt;
            }
            else if (int.TryParse(arguments[0]?.ToString(), out int parsedWidth))
            {
                totalWidth = parsedWidth;
            }
            
            // Get padding character (optional, default is space)
            char paddingChar = ' ';
            if (arguments.Count >= 2)
            {
                var paddingStr = arguments[1]?.ToString() ?? " ";
                if (paddingStr.Length > 0)
                {
                    paddingChar = paddingStr[0];
                }
            }
            
            return target.PadRight(totalWidth, paddingChar);
        }

        private string EvaluateSlice(SliceNode node, EvaluationContext context)
        {
            // Evaluate target first
            var targetValue = EvaluateNode(node.Target, context);
            
            // Evaluate start and end indices
            int? startIndex = null;
            int? endIndex = null;
            
            if (node.Start != null)
            {
                if (node.Start is LiteralNode startLiteral && startLiteral.Value is int startInt)
                {
                    startIndex = startInt;
                }
                else
                {
                    var startValue = EvaluateNode(node.Start, context);
                    if (int.TryParse(startValue, out int parsedStart))
                    {
                        startIndex = parsedStart;
                    }
                }
            }
            
            if (node.End != null)
            {
                if (node.End is LiteralNode endLiteral && endLiteral.Value is int endInt)
                {
                    endIndex = endInt;
                }
                else
                {
                    var endValue = EvaluateNode(node.End, context);
                    if (int.TryParse(endValue, out int parsedEnd))
                    {
                        endIndex = parsedEnd;
                    }
                }
            }
            
            // Handle slice indices (Python-style)
            int start = startIndex ?? 0;
            int end = endIndex ?? targetValue.Length;
            
            // Handle negative indices
            if (start < 0)
            {
                start = targetValue.Length + start;
            }
            if (end < 0)
            {
                end = targetValue.Length + end;
            }
            
            // Clamp to valid range
            start = Math.Max(0, Math.Min(start, targetValue.Length));
            end = Math.Max(0, Math.Min(end, targetValue.Length));
            
            // Extract substring
            if (start >= end)
            {
                return ""; // Invalid range
            }
            
            return targetValue.Substring(start, end - start);
        }

        /// <summary>
        /// Evaluate simple arithmetic expression with variable 'i'
        /// Supports: +, -, *, / operators
        /// Example: "2*i+1", "2i+1", "i*3-2", "i/2+10"
        /// </summary>
        private int EvaluateExpression(string expression, int indexValue)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return indexValue;
            }

            try
            {
                // Remove whitespace
                expression = expression.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
                
                if (string.IsNullOrEmpty(expression))
                {
                    return indexValue;
                }

                // Normalize expression: replace implicit multiplication (e.g., "2i" -> "2*i")
                // Match patterns like: digit followed by 'i', or 'i' followed by digit
                expression = Regex.Replace(expression, @"(\d+)([iI])", "$1*$2", RegexOptions.IgnoreCase);
                expression = Regex.Replace(expression, @"([iI])(\d+)", "$1*$2", RegexOptions.IgnoreCase);
                
                // Replace 'i' with the actual index value
                // Use regex to replace 'i' but not as part of other identifiers
                expression = Regex.Replace(expression, @"\b[iI]\b", indexValue.ToString(), RegexOptions.IgnoreCase);
                
                // Validate expression contains only numbers, operators, and parentheses
                if (!Regex.IsMatch(expression, @"^[0-9+\-*/().\s]+$"))
                {
                    throw new ArgumentException($"Invalid expression: {expression}");
                }

                // Use DataTable to evaluate the expression safely
                var dataTable = new DataTable();
                var result = dataTable.Compute(expression, null);
                
                // Convert to integer
                return Convert.ToInt32(Math.Round(Convert.ToDouble(result)));
            }
            catch (Exception ex)
            {
                // If evaluation fails, return original index
                // In production, you might want to log this error
                return indexValue;
            }
        }
    }
}
