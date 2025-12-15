using System;
using System.Collections.Generic;
using System.IO;
using BatchRenameTool.Template.Evaluator;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents a file value in the template expression system
    /// </summary>
    public class FileValue : ITemplateValue
    {
        private readonly IEvaluationContext _context;

        public FileValue(IEvaluationContext context)
        {
            _context = context;
        }

        public object? GetValue() => _context.File;

        public string ToString(string? format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                return _context.FullPath;
            }

            var file = _context.File;
            if (!file.Exists)
            {
                return "";
            }

            return format.ToLower() switch
            {
                "createtime" or "createtime" => file.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                "edittime" or "edittime" or "lastwritetime" => file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                "accesstime" or "lastaccesstime" => file.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => file.CreationTime.ToString(format) // Try to use formatString as DateTime format
            };
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
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported on FileValue")
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

