using System;
using System.Collections.Generic;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents a file size value in the template expression system
    /// </summary>
    public class SizeValue : ITemplateValue
    {
        private readonly long _sizeBytes;

        public SizeValue(long sizeBytes)
        {
            _sizeBytes = sizeBytes;
        }

        public object? GetValue() => _sizeBytes;

        public string ToString(string? format = null)
        {
            if (_sizeBytes == 0)
            {
                return "0 B";
            }

            if (string.IsNullOrEmpty(format))
            {
                return FormatFileSizeAuto(_sizeBytes, 2); // Default: auto format with 2 decimals
            }

            // Check for specific unit format (1b, 1kb, 1mb)
            if (format.Equals("1b", StringComparison.OrdinalIgnoreCase))
            {
                return $"{_sizeBytes} B";
            }

            if (format.Equals("1kb", StringComparison.OrdinalIgnoreCase))
            {
                return $"{_sizeBytes / 1024.0:F0} KB";
            }

            if (format.Equals("1mb", StringComparison.OrdinalIgnoreCase))
            {
                return $"{_sizeBytes / (1024.0 * 1024.0):F2} MB";
            }

            // Check for auto format with decimal places (.2f, .1f, .0f)
            if (format.StartsWith(".") && format.EndsWith("f"))
            {
                var decimalPart = format.Substring(1, format.Length - 2);
                if (int.TryParse(decimalPart, out int decimals))
                {
                    return FormatFileSizeAuto(_sizeBytes, decimals);
                }
            }

            // Default: auto format
            return FormatFileSizeAuto(_sizeBytes, 2);
        }

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

        public bool HasMethod(string methodName)
        {
            var method = methodName.ToLower();
            return method switch
            {
                "format" => true,
                _ => false
            };
        }

        public ITemplateValue InvokeMethod(string methodName, IReadOnlyList<ITemplateValue> arguments)
        {
            var method = methodName.ToLower();
            return method switch
            {
                "format" => ExecuteFormat(arguments),
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported on SizeValue")
            };
        }

        private ITemplateValue ExecuteFormat(IReadOnlyList<ITemplateValue> arguments)
        {
            string format = "";
            if (arguments.Count > 0)
            {
                format = arguments[0].ToString();
            }

            return new StringValue(ToString(format));
        }

        public override string ToString() => ToString(null);
    }
}

