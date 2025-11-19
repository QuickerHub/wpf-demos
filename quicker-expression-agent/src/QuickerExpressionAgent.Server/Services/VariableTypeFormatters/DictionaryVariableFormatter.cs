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
}

