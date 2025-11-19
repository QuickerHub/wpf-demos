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

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return new List<string>();
        }

        // If already List<string>, return as-is
        if (value is List<string> stringList)
        {
            return stringList;
        }

        // If JsonElement, extract List<string> value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray()
                    .Select(e => e.ValueKind == System.Text.Json.JsonValueKind.String 
                        ? e.GetString() ?? string.Empty 
                        : e.ToString())
                    .ToList();
            }
            return new List<string>();
        }

        // If IEnumerable<string>, convert to List<string>
        if (value is System.Collections.Generic.IEnumerable<string> stringEnumerable)
        {
            return stringEnumerable.ToList();
        }

        // If IEnumerable<object>, convert each element to string
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            return enumerable.Cast<object>()
                .Select(item => item?.ToString() ?? string.Empty)
                .ToList();
        }

        // If string that looks like JSON array, try to parse
        if (value is string jsonString && jsonString.TrimStart().StartsWith('['))
        {
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    return jsonDoc.RootElement.EnumerateArray()
                        .Select(e => e.ValueKind == System.Text.Json.JsonValueKind.String 
                            ? e.GetString() ?? string.Empty 
                            : e.ToString())
                        .ToList();
                }
            }
            catch
            {
                // If parsing fails, treat as single string item
            }
        }

        // If string, treat as single item list
        if (value is string str)
        {
            return new List<string> { str };
        }

        return new List<string>();
    }
}

