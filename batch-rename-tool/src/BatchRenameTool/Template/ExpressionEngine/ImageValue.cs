using System;
using System.Collections.Generic;
using BatchRenameTool.Template.Evaluator;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Represents an image value in the template expression system
    /// </summary>
    public class ImageValue : ITemplateValue
    {
        private readonly IImageInfo _image;

        public ImageValue(IImageInfo image)
        {
            _image = image;
        }

        public object? GetValue() => _image;

        public string ToString(string? format = null)
        {
            if (_image.Width == 0 && _image.Height == 0)
            {
                return ""; // Not an image or failed to load
            }

            if (string.IsNullOrEmpty(format))
            {
                return $"{_image.Width}x{_image.Height}"; // Default: wxh format
            }

            return format.ToLower() switch
            {
                "w" => _image.Width.ToString(),
                "h" => _image.Height.ToString(),
                "wxh" => $"{_image.Width}x{_image.Height}",
                _ => $"{_image.Width}x{_image.Height}" // Default fallback
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
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported on ImageValue")
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

