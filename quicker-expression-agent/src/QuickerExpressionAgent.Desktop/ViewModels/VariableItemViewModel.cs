using CommunityToolkit.Mvvm.ComponentModel;
using QuickerExpressionAgent.Common;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for a single variable item in the variable list
/// </summary>
public partial class VariableItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _varName = string.Empty;

    [ObservableProperty]
    private VariableType _varType = VariableType.String;

    [ObservableProperty]
    private string _valueText = string.Empty;

    [ObservableProperty]
    private bool _isListType = false;

    [ObservableProperty]
    private bool _isDictionaryType = false;

    /// <summary>
    /// Event triggered when value text changes
    /// </summary>
    public event EventHandler? ValueChanged;

    public VariableItemViewModel()
    {
    }

    public VariableItemViewModel(VariableClass variable)
    {
        VarName = variable.VarName;
        VarType = variable.VarType;
        IsListType = variable.VarType == VariableType.ListString;
        IsDictionaryType = variable.VarType == VariableType.Dictionary;

        // Convert default value to string representation
        ValueText = ConvertValueToString(variable.DefaultValue, variable.VarType);
    }

    /// <summary>
    /// Update default value from a VariableClass (used when regenerating expressions)
    /// Only updates if the current value matches the old default value (user hasn't modified it)
    /// </summary>
    public void UpdateDefaultValueIfUnchanged(VariableClass newVariable, object? oldDefaultValue)
    {
        // Check if current value matches the old default value
        var currentValue = ConvertStringToValue();
        var oldDefaultValueStr = ConvertValueToString(oldDefaultValue, VarType);
        var currentValueStr = ConvertValueToString(currentValue, VarType);

        // If current value matches old default, update to new default
        if (currentValueStr == oldDefaultValueStr ||
            (currentValue == null && oldDefaultValue == null) ||
            (currentValue != null && currentValue.Equals(oldDefaultValue)))
        {
            // User hasn't modified the value, update to new default
            ValueText = ConvertValueToString(newVariable.DefaultValue, newVariable.VarType);
        }
        // Otherwise, keep user's modified value
    }

    partial void OnValueTextChanged(string value)
    {
        // Trigger value changed event when value text changes
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Convert value to string for display/editing
    /// </summary>
    private string ConvertValueToString(object? value, VariableType varType)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (varType == VariableType.ListString)
        {
            if (value is System.Collections.IEnumerable enumerable)
            {
                var items = enumerable.Cast<object>().Select(item => item?.ToString() ?? "").ToList();
                return string.Join("\n", items);
            }
            return string.Empty;
        }

        if (varType == VariableType.Dictionary)
        {
            if (value is System.Collections.IDictionary dict)
            {
                // Convert Dictionary to JSON format
                var jsonDict = new Dictionary<string, object?>();
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    jsonDict[entry.Key?.ToString() ?? ""] = entry.Value;
                }
                return JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
            }
            return "{}";
        }

        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Convert string back to object value
    /// </summary>
    public object? ConvertStringToValue()
    {
        switch (VarType)
        {
            case VariableType.ListString:
                // Parse by lines, keep all lines including empty ones
                // Handle different line endings: \r\n, \n, \r using regex
                var items = Regex.Split(ValueText, @"\r\n|\r|\n")
                    .Select(line => line.TrimEnd())  // Only trim trailing whitespace, keep leading spaces and empty lines
                    .ToList();
                return items;

            case VariableType.Dictionary:
                // Parse JSON format
                try
                {
                    var trimmedText = ValueText.Trim();
                    if (string.IsNullOrEmpty(trimmedText))
                    {
                        return new Dictionary<string, object>();
                    }
                    
                    // Try to parse as JSON
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(trimmedText);
                    
                    // Convert JsonElement to Dictionary<string, object>
                    var dict = new Dictionary<string, object?>();
                    if (jsonElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            dict[property.Name] = ConvertJsonElementToObject(property.Value);
                        }
                    }
                    return dict;
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, return empty dictionary
                    return new Dictionary<string, object>();
                }

            case VariableType.String:
                return ValueText;

            case VariableType.Int:
                if (int.TryParse(ValueText, out var intValue))
                {
                    return intValue;
                }
                return 0;

            case VariableType.Double:
                if (double.TryParse(ValueText, out var doubleValue))
                {
                    return doubleValue;
                }
                return 0.0;

            case VariableType.Bool:
                if (bool.TryParse(ValueText, out var boolValue))
                {
                    return boolValue;
                }
                return false;

            case VariableType.DateTime:
                if (DateTime.TryParse(ValueText, out var dateTimeValue))
                {
                    return dateTimeValue;
                }
                return DateTime.Now;

            case VariableType.Object:
            default:
                return ValueText;
        }
    }

    /// <summary>
    /// Convert JsonElement to object (recursive for nested structures)
    /// </summary>
    private object? ConvertJsonElementToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var intValue))
                {
                    return intValue;
                }
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(ConvertJsonElementToObject).ToList();
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElementToObject(prop.Value);
                }
                return dict;
            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Convert to VariableClass for execution
    /// </summary>
    public VariableClass ToVariableClass()
    {
        return new VariableClass
        {
            VarName = VarName,
            VarType = VarType,
            DefaultValue = ConvertStringToValue() ?? new object()
        };
    }
}

