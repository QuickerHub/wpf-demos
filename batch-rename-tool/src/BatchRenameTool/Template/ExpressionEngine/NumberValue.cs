using System;
using System.Collections.Generic;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents a number value in the template expression system
    /// </summary>
    public class NumberValue : ITemplateValue
    {
        private readonly int _value;

        public NumberValue(int value)
        {
            _value = value;
        }

        public object? GetValue() => _value;

        public virtual string ToString(string? format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                return _value.ToString();
            }

            // Number formatting (basic support)
            // More complex formatting can be added later
            return _value.ToString(format);
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
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported on NumberValue")
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

        public override string ToString() => _value.ToString();
    }
}

