using System;
using System.Collections.Generic;
using System.Linq;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents a string value in the template expression system
    /// Provides string manipulation methods
    /// </summary>
    public class StringValue : ITemplateValue
    {
        private readonly string _value;

        public StringValue(string value)
        {
            _value = value ?? "";
        }

        public object? GetValue() => _value;

        public string ToString(string? format = null)
        {
            // String formatting (if needed in future)
            return _value;
        }

        public bool HasMethod(string methodName)
        {
            var method = methodName.ToLower();
            return method switch
            {
                "upper" => true,
                "lower" => true,
                "trim" => true,
                "replace" => true,
                "sub" => true,
                "padleft" => true,
                "padright" => true,
                "slice" => true,
                _ => false
            };
        }

        public ITemplateValue InvokeMethod(string methodName, IReadOnlyList<ITemplateValue> arguments)
        {
            var method = methodName.ToLower();
            return method switch
            {
                "upper" => new StringValue(_value.ToUpper()),
                "lower" => new StringValue(_value.ToLower()),
                "trim" => new StringValue(_value.Trim()),
                "replace" => ExecuteReplace(arguments),
                "sub" => ExecuteSub(arguments),
                "padleft" => ExecutePadLeft(arguments),
                "padright" => ExecutePadRight(arguments),
                "slice" => ExecuteSlice(arguments),
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported on StringValue")
            };
        }

        private ITemplateValue ExecuteReplace(IReadOnlyList<ITemplateValue> arguments)
        {
            if (arguments.Count < 2)
            {
                return this; // Not enough arguments
            }

            var oldValue = arguments[0].ToString();
            var newValue = arguments[1].ToString();
            return new StringValue(_value.Replace(oldValue, newValue));
        }

        private ITemplateValue ExecuteSub(IReadOnlyList<ITemplateValue> arguments)
        {
            if (arguments.Count == 0)
            {
                return this;
            }

            var startObj = arguments[0].GetValue();
            int start = startObj is int startInt ? startInt :
                       (int.TryParse(startObj?.ToString(), out int parsed) ? parsed : 0);

            if (start < 0) start = _value.Length + start;
            start = Math.Max(0, Math.Min(start, _value.Length));

            if (arguments.Count >= 2)
            {
                var endObj = arguments[1].GetValue();
                int end = endObj is int endInt ? endInt :
                         (int.TryParse(endObj?.ToString(), out int parsedEnd) ? parsedEnd : _value.Length);

                if (end < 0) end = _value.Length + end;
                end = Math.Max(0, Math.Min(end, _value.Length));

                if (start >= end) return new StringValue("");
                return new StringValue(_value.Substring(start, end - start));
            }
            else
            {
                return new StringValue(_value.Substring(start));
            }
        }

        private ITemplateValue ExecutePadLeft(IReadOnlyList<ITemplateValue> arguments)
        {
            if (arguments.Count == 0)
            {
                return this;
            }

            var widthObj = arguments[0].GetValue();
            int totalWidth = widthObj is int widthInt ? widthInt :
                            (int.TryParse(widthObj?.ToString(), out int parsed) ? parsed : _value.Length);

            char paddingChar = ' ';
            if (arguments.Count >= 2)
            {
                var paddingStr = arguments[1].ToString();
                if (paddingStr.Length > 0) paddingChar = paddingStr[0];
            }

            return new StringValue(_value.PadLeft(totalWidth, paddingChar));
        }

        private ITemplateValue ExecutePadRight(IReadOnlyList<ITemplateValue> arguments)
        {
            if (arguments.Count == 0)
            {
                return this;
            }

            var widthObj = arguments[0].GetValue();
            int totalWidth = widthObj is int widthInt ? widthInt :
                            (int.TryParse(widthObj?.ToString(), out int parsed) ? parsed : _value.Length);

            char paddingChar = ' ';
            if (arguments.Count >= 2)
            {
                var paddingStr = arguments[1].ToString();
                if (paddingStr.Length > 0) paddingChar = paddingStr[0];
            }

            return new StringValue(_value.PadRight(totalWidth, paddingChar));
        }

        private ITemplateValue ExecuteSlice(IReadOnlyList<ITemplateValue> arguments)
        {
            int? start = null;
            int? end = null;

            if (arguments.Count > 0)
            {
                var startObj = arguments[0].GetValue();
                start = startObj is int startInt ? startInt :
                       (int.TryParse(startObj?.ToString(), out int parsed) ? parsed : (int?)null);
            }

            if (arguments.Count > 1)
            {
                var endObj = arguments[1].GetValue();
                end = endObj is int endInt ? endInt :
                     (int.TryParse(endObj?.ToString(), out int parsed) ? parsed : (int?)null);
            }

            int startIndex = start ?? 0;
            int endIndex = end ?? _value.Length;

            if (startIndex < 0) startIndex = _value.Length + startIndex;
            if (endIndex < 0) endIndex = _value.Length + endIndex;

            startIndex = Math.Max(0, Math.Min(startIndex, _value.Length));
            endIndex = Math.Max(0, Math.Min(endIndex, _value.Length));

            if (startIndex >= endIndex)
            {
                return new StringValue("");
            }

            return new StringValue(_value.Substring(startIndex, endIndex - startIndex));
        }

        public override string ToString() => _value;
    }
}

