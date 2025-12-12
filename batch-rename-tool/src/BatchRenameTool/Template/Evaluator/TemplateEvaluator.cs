using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Utils;

namespace BatchRenameTool.Template.Evaluator
{
    /// <summary>
    /// Implementation of evaluation context with lazy loading for file properties
    /// </summary>
    public class EvaluationContext : IEvaluationContext
    {
        private readonly Lazy<FileInfo> _file;
#if WPF
        private readonly ViewModels.FileRenameItem? _fileRenameItem;
#endif

        public string Name { get; }
        public string Ext { get; }
        public string FullName { get; }
        public string FullPath { get; }
        public int Index { get; }
        public int TotalCount { get; }
        public DateTime Today { get; }
        public DateTime Now { get; }

#if WPF
        public IImageInfo Image => _fileRenameItem?.Image ?? new ImageInfo(FullPath);
#else
        public IImageInfo Image => new ImageInfo(FullPath);
#endif
        public FileInfo File => _file.Value;
        public long Size => File.Exists ? File.Length : 0;

        /// <summary>
        /// Constructor for evaluation context
        /// </summary>
        public EvaluationContext(
            string name,
            string ext,
            string fullName,
            string fullPath,
            int index,
            int totalCount
#if WPF
            , ViewModels.FileRenameItem? fileRenameItem = null
#endif
            )
        {
            Name = name;
            Ext = ext;
            FullName = fullName;
            FullPath = fullPath;
            Index = index;
            TotalCount = totalCount;
            Today = DateTime.Today;
            Now = DateTime.Now;
#if WPF
            _fileRenameItem = fileRenameItem;
#endif

            // Initialize lazy FileInfo - System.IO.FileInfo
            _file = new Lazy<FileInfo>(
                () => new FileInfo(fullPath),
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }

    /// <summary>
    /// Evaluator for template AST
    /// </summary>
    public class TemplateEvaluator
    {
        /// <summary>
        /// Evaluate template node with context
        /// </summary>
        public string Evaluate(TemplateNode node, IEvaluationContext context)
        {
            var sb = new StringBuilder();

            foreach (var astNode in node.Nodes)
            {
                sb.Append(EvaluateNode(astNode, context));
            }

            return sb.ToString();
        }

        private string EvaluateNode(AstNode node, IEvaluationContext context)
        {
            return node switch
            {
                TextNode textNode => textNode.Text,
                VariableNode varNode => EvaluateVariable(varNode, context),
                FormatNode formatNode => EvaluateFormat(formatNode, context),
                MethodNode methodNode => EvaluateMethod(methodNode, context),
                SliceNode sliceNode => EvaluateSlice(sliceNode, context),
                _ => $"[????????: {node.GetType().Name}]"
            };
        }

        private string EvaluateVariable(VariableNode node, IEvaluationContext context)
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
                "image" => FormatImage(context.Image, ""), // Default: wxh format
                "file" => context.FullPath, // Default: file path
                "size" => FormatFileSize(context.Size, ""), // Default: auto format
                _ => $"{{{node.VariableName}}}"
            };
        }

        private string EvaluateFormat(FormatNode node, IEvaluationContext context)
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
                
                // Handle image variable (image)
                if (varName == "image")
                {
                    return FormatImage(context.Image, node.FormatString);
                }
                
                // Handle file variable (file)
                if (varName == "file")
                {
                    return FormatFile(context, node.FormatString);
                }
                
                // Handle size variable (size)
                if (varName == "size")
                {
                    return FormatFileSize(context.Size, node.FormatString);
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

            // Special case: "?" or "?" means start from 10
            if (formatString[0] == '?' || formatString[0] == '?')
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

        /// <summary>
        /// Format image dimensions
        /// </summary>
        private string FormatImage(IImageInfo image, string formatString)
        {
            if (image.Width == 0 && image.Height == 0)
            {
                return ""; // Not an image or failed to load
            }

            if (string.IsNullOrEmpty(formatString))
            {
                return $"{image.Width}x{image.Height}"; // Default: wxh format
            }

            return formatString.ToLower() switch
            {
                "w" => image.Width.ToString(),
                "h" => image.Height.ToString(),
                "wxh" => $"{image.Width}x{image.Height}",
                _ => $"{image.Width}x{image.Height}" // Default fallback
            };
        }

        /// <summary>
        /// Format file variable (supports file.createTime, file.editTime, etc.)
        /// </summary>
        private string FormatFile(IEvaluationContext context, string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                return context.FullPath;
            }

            var file = context.File;
            if (!file.Exists)
            {
                return "";
            }

            // Parse formatString to support file.createTime, file.editTime, etc.
            return formatString.ToLower() switch
            {
                "createtime" or "createtime" => file.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                "edittime" or "edittime" or "lastwritetime" => file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                "accesstime" or "lastaccesstime" => file.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => file.CreationTime.ToString(formatString) // Try to use formatString as DateTime format
            };
        }

        /// <summary>
        /// Format file size with various units
        /// </summary>
        private string FormatFileSize(long sizeBytes, string formatString)
        {
            if (sizeBytes == 0)
            {
                return "0 B";
            }

            if (string.IsNullOrEmpty(formatString))
            {
                return FormatFileSizeAuto(sizeBytes, 2); // Default: auto format with 2 decimals
            }

            // Check for specific unit format (1b, 1kb, 1mb)
            if (formatString.Equals("1b", StringComparison.OrdinalIgnoreCase))
            {
                return $"{sizeBytes} B";
            }

            if (formatString.Equals("1kb", StringComparison.OrdinalIgnoreCase))
            {
                return $"{sizeBytes / 1024.0:F0} KB";
            }

            if (formatString.Equals("1mb", StringComparison.OrdinalIgnoreCase))
            {
                return $"{sizeBytes / (1024.0 * 1024.0):F2} MB";
            }

            // Check for auto format with decimal places (.2f, .1f, .0f)
            if (formatString.StartsWith(".") && formatString.EndsWith("f"))
            {
                var decimalPart = formatString.Substring(1, formatString.Length - 2);
                if (int.TryParse(decimalPart, out int decimals))
                {
                    return FormatFileSizeAuto(sizeBytes, decimals);
                }
            }

            // Default: auto format
            return FormatFileSizeAuto(sizeBytes, 2);
        }

        /// <summary>
        /// Format file size with automatic unit selection
        /// </summary>
        private string FormatFileSizeAuto(long sizeBytes, int decimals)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = sizeBytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            string format = $"F{decimals}";
            return $"{size.ToString(format)} {units[unitIndex]}";
        }

        private string EvaluateMethod(MethodNode node, IEvaluationContext context)
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
                _ => $"[????: {node.MethodName}]"
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

        private string EvaluateSlice(SliceNode node, IEvaluationContext context)
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
