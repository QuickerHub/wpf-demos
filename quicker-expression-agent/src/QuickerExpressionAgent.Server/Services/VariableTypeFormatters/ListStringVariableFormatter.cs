using System.Linq;

namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for List<string> type variables
/// </summary>
public class ListStringVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "List<string>";

    public string FormatDefaultValue() => "new List<string>()";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            var items = enumerable.Cast<object>().Select(item => $"\"{item}\"").ToList();
            return $"new List<string>() {{ {string.Join(", ", items)} }}";
        }

        return FormatDefaultValue();
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var items = enumerable.Cast<object>().Select(item => 
            {
                if (item is string str)
                {
                    return $"\"{str.Replace("\"", "\\\"")}\"";
                }
                return item?.ToString() ?? "null";
            }).ToList();
            return $"new List<object>() {{ {string.Join(", ", items)} }}";
        }

        return "null";
    }
}

