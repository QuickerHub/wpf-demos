using System;
using System.Collections.Generic;
using BatchRenameTool.Template.Evaluator;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents a date/time value in the template expression system
    /// </summary>
    public class DateValue : ITemplateValue
    {
        private readonly DateTime _value;

        public DateValue(DateTime value)
        {
            _value = value;
        }

        public object? GetValue() => _value;

        public string ToString(string? format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                // Default format based on whether it's date only or datetime
                return _value.TimeOfDay == TimeSpan.Zero 
                    ? _value.ToString("yyyy-MM-dd")
                    : _value.ToString("yyyy-MM-dd HH:mm:ss");
            }

            try
            {
                return _value.ToString(format);
            }
            catch
            {
                // If format string is invalid, return default format
                return _value.TimeOfDay == TimeSpan.Zero
                    ? _value.ToString("yyyy-MM-dd")
                    : _value.ToString("yyyy-MM-dd HH:mm:ss");
            }
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
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported on DateValue")
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

