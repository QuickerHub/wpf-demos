using System.Linq;

namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for Dictionary<string, object> type variables
/// </summary>
public class DictionaryVariableFormatter : IVariableTypeFormatter
{
    private readonly VariableTypeFormatterFactory _formatterFactory;

    public DictionaryVariableFormatter(VariableTypeFormatterFactory formatterFactory)
    {
        _formatterFactory = formatterFactory;
    }

    public string GetTypeDeclaration() => "Dictionary<string, object>";

    public string FormatDefaultValue() => "new Dictionary<string, object>()";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is System.Collections.IDictionary dict)
        {
            var items = new List<string>();
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? "";
                var val = FormatValueForDictionary(entry.Value);
                items.Add($"[\"{key}\"] = {val}");
            }
            return $"new Dictionary<string, object>() {{ {string.Join(", ", items)} }}";
        }

        return FormatDefaultValue();
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        // Handle nested Dictionary
        if (value is System.Collections.IDictionary nestedDict)
        {
            var items = new List<string>();
            foreach (System.Collections.DictionaryEntry entry in nestedDict)
            {
                var key = entry.Key?.ToString() ?? "";
                var val = FormatValueForDictionary(entry.Value);
                items.Add($"[\"{key}\"] = {val}");
            }
            return $"new Dictionary<string, object>() {{ {string.Join(", ", items)} }}";
        }

        // Handle List
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var items = enumerable.Cast<object>().Select(FormatValueForDictionary).ToList();
            return $"new List<object>() {{ {string.Join(", ", items)} }}";
        }

        // Use appropriate formatter based on value type
        var formatter = _formatterFactory.GetFormatterForValue(value);
        return formatter.FormatValueForDictionary(value);
    }

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return new Dictionary<string, object>();
        }

        // If already Dictionary<string, object>, return as-is
        if (value is Dictionary<string, object> dict)
        {
            return dict;
        }

        // If JsonElement, extract Dictionary value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var result = new Dictionary<string, object>();
                foreach (var prop in jsonElement.EnumerateObject())
                {
                    result[prop.Name] = ConvertJsonElementToObject(prop.Value);
                }
                return result;
            }
            return new Dictionary<string, object>();
        }

        // If IDictionary, convert to Dictionary<string, object>
        if (value is System.Collections.IDictionary idict)
        {
            var result = new Dictionary<string, object>();
            foreach (System.Collections.DictionaryEntry entry in idict)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                result[key] = entry.Value ?? new object();
            }
            return result;
        }

        // If string that looks like JSON object, try to parse
        if (value is string jsonString && jsonString.TrimStart().StartsWith('{'))
        {
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var result = new Dictionary<string, object>();
                    foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                    {
                        result[prop.Name] = ConvertJsonElementToObject(prop.Value);
                    }
                    return result;
                }
            }
            catch
            {
                // If parsing fails, return empty dictionary
            }
        }

        return new Dictionary<string, object>();
    }

    private object ConvertJsonElementToObject(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case System.Text.Json.JsonValueKind.Number:
                return element.TryGetInt32(out var intVal) ? intVal : element.GetDouble();
            case System.Text.Json.JsonValueKind.True:
                return true;
            case System.Text.Json.JsonValueKind.False:
                return false;
            case System.Text.Json.JsonValueKind.Null:
                return null!;
            case System.Text.Json.JsonValueKind.Array:
                return element.EnumerateArray().Select(ConvertJsonElementToObject).ToList();
            case System.Text.Json.JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElementToObject(prop.Value);
                }
                return dict;
            default:
                return element.ToString();
        }
    }
}

