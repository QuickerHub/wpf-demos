using System.Text.Json;
using System.Linq;

namespace QuickerExpressionAgent.Common;

/// <summary>
/// Extension methods for VariableType enum
/// </summary>
public static class VariableTypeExtensions
{
    /// <summary>
    /// Get the C# type declaration string for this variable type
    /// </summary>
    public static string GetTypeDeclaration(this VariableType varType) => varType switch
    {
        VariableType.String => "string",
        VariableType.Int => "int",
        VariableType.Double => "double",
        VariableType.Bool => "bool",
        VariableType.DateTime => "DateTime",
        VariableType.ListString => "List<string>",
        VariableType.Dictionary => "Dictionary<string, object>",
        VariableType.Object => "object",
        _ => "object"
    };

    /// <summary>
    /// Get default value for this variable type
    /// </summary>
    public static object GetDefaultValue(this VariableType varType) => varType switch
    {
        VariableType.String => string.Empty,
        VariableType.Int => 0,
        VariableType.Double => 0.0,
        VariableType.Bool => false,
        VariableType.DateTime => DateTime.Now,
        VariableType.ListString => new List<string>(),
        VariableType.Dictionary => new Dictionary<string, object>(),
        VariableType.Object => new object(),
        _ => new object()
    };

    /// <summary>
    /// Get default value for this variable type, or parse the provided value string if available
    /// </summary>
    public static object GetDefaultValue(this VariableType varType, string? providedValue)
    {
        if (string.IsNullOrEmpty(providedValue))
            return varType.GetDefaultValue();

        try
        {
            return varType.ConvertValueFromString(providedValue);
        }
        catch
        {
            return varType.GetDefaultValue();
        }
    }

    /// <summary>
    /// Convert a string value to the appropriate type for this VariableType
    /// </summary>
    public static object ConvertValueFromString(this VariableType varType, string? value)
    {
        if (value == null)
            return varType.GetDefaultValue();

        return varType switch
        {
            VariableType.String => value,
            VariableType.Int => int.Parse(value),
            VariableType.Double => double.Parse(value),
            VariableType.Bool => bool.Parse(value),
            VariableType.DateTime => DateTime.Parse(value),
            VariableType.ListString => TryParseListString(value),
            VariableType.Dictionary => TryParseDictionary(value),
            VariableType.Object => value,
            _ => value
        };
    }

    /// <summary>
    /// Try to convert a string value to the appropriate type, returning default if parsing fails
    /// </summary>
    public static object ConvertValueFromStringSafe(this VariableType varType, string? value)
    {
        try
        {
            return varType.ConvertValueFromString(value);
        }
        catch
        {
            return varType.GetDefaultValue();
        }
    }

    /// <summary>
    /// Try to parse ListString from string
    /// First attempts JSON array format, then falls back to newline-separated format
    /// </summary>
    private static List<string> TryParseListString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        // First, try to parse as JSON array
        try
        {
            var trimmedValue = value.Trim();
            if (trimmedValue.StartsWith("[") && trimmedValue.EndsWith("]"))
            {
                var jsonDoc = JsonDocument.Parse(trimmedValue);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var element in jsonDoc.RootElement.EnumerateArray())
                    {
                        list.Add(element.ValueKind == JsonValueKind.String 
                            ? element.GetString() ?? string.Empty 
                            : element.ToString());
                    }
                    return list;
                }
            }
        }
        catch
        {
            // JSON parsing failed, fall through to newline-separated format
        }

        // Fall back to newline-separated format
        return value.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
            .Select(line => line.TrimEnd())
            .ToList();
    }

    /// <summary>
    /// Try to parse dictionary from JSON string
    /// </summary>
    private static Dictionary<string, object> TryParseDictionary(string jsonStr)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonStr);
            var dict = new Dictionary<string, object>();

            foreach (var prop in jsonDoc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = ConvertJsonElementToObject(prop.Value);
            }

            return dict;
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Convert JsonElement to object recursively
    /// </summary>
    private static object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => new object(),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElementToObject(p.Value)),
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Convert JsonElement to appropriate .NET type based on VariableType
    /// </summary>
    public static object ConvertValueFromJson(this VariableType varType, JsonElement jsonElement)
    {
        return varType switch
        {
            VariableType.String => jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString() ?? string.Empty
                : jsonElement.ToString(),
            VariableType.Int => jsonElement.ValueKind == JsonValueKind.Number
                ? (jsonElement.TryGetInt32(out var intVal) ? intVal : (int)jsonElement.GetDouble())
                : 0,
            VariableType.Double => jsonElement.ValueKind == JsonValueKind.Number
                ? jsonElement.GetDouble()
                : 0.0,
            VariableType.Bool => jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False
                ? jsonElement.GetBoolean()
                : false,
            VariableType.DateTime => jsonElement.ValueKind == JsonValueKind.String
                ? DateTime.Parse(jsonElement.GetString() ?? DateTime.Now.ToString())
                : DateTime.Now,
            VariableType.ListString => jsonElement.ValueKind == JsonValueKind.Array
                ? jsonElement.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? string.Empty : e.ToString())
                    .ToList()
                : new List<string>(),
            VariableType.Dictionary => jsonElement.ValueKind == JsonValueKind.Object
                ? jsonElement.EnumerateObject().ToDictionary(
                    p => p.Name,
                    p => ConvertJsonElementToObject(p.Value))
                : new Dictionary<string, object>(),
            VariableType.Object => ConvertJsonElementToObject(jsonElement),
            _ => ConvertJsonElementToObject(jsonElement)
        };
    }

    /// <summary>
    /// Convert System.Type to VariableType enum
    /// </summary>
    public static VariableType ConvertToVariableType(this Type? type)
    {
        if (type == null)
            return VariableType.Object;

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            if (genericDef == typeof(List<>) && genericArgs.Length == 1 && genericArgs[0] == typeof(string))
                return VariableType.ListString;

            if (genericDef == typeof(Dictionary<,>) && genericArgs.Length == 2 &&
                genericArgs[0] == typeof(string) && genericArgs[1] == typeof(object))
                return VariableType.Dictionary;
        }

        // Handle non-generic types
        return MatchTypeName(type.Name);
    }

    /// <summary>
    /// Convert C# type name string to VariableType enum
    /// </summary>
    public static VariableType FromTypeName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return VariableType.Object;

        var normalized = typeName.Trim();

        // Handle generic types with angle brackets: List<string>, Dictionary<string, object>
        if (normalized.Contains('<') && normalized.Contains('>'))
        {
            var genericStart = normalized.IndexOf('<');
            var genericName = normalized.Substring(0, genericStart).Trim();
            var genericArgs = normalized.Substring(genericStart + 1, normalized.LastIndexOf('>') - genericStart - 1).Trim();

            // Remove backtick and number suffix (e.g., "List`1" -> "List")
            genericName = RemoveGenericSuffix(genericName);

            if (IsListType(genericName) && IsStringType(genericArgs))
                return VariableType.ListString;

            if (IsDictionaryType(genericName))
            {
                var args = genericArgs.Split(',');
                if (args.Length >= 2 && IsStringType(args[0].Trim()) && IsObjectType(args[1].Trim()))
                    return VariableType.Dictionary;
            }
        }
        // Handle generic types with backtick format: List`1[System.String], Dictionary`2[System.String,System.Object]
        else if (normalized.Contains('`') && normalized.Contains('['))
        {
            var backtickIndex = normalized.IndexOf('`');
            var bracketStart = normalized.IndexOf('[');
            var genericName = normalized.Substring(0, backtickIndex).Trim();
            var genericArgs = normalized.Substring(bracketStart + 1, normalized.LastIndexOf(']') - bracketStart - 1).Trim();

            if (IsListType(genericName) && IsStringType(genericArgs))
                return VariableType.ListString;

            if (IsDictionaryType(genericName))
            {
                var args = genericArgs.Split(',');
                if (args.Length >= 2 && IsStringType(args[0].Trim()) && IsObjectType(args[1].Trim()))
                    return VariableType.Dictionary;
            }
        }

        // Handle non-generic types (remove backtick suffix if present)
        var typeNameWithoutSuffix = RemoveGenericSuffix(normalized);
        return MatchTypeName(typeNameWithoutSuffix);
    }

    /// <summary>
    /// Remove generic type suffix (e.g., "List`1" -> "List", "Dictionary`2" -> "Dictionary")
    /// </summary>
    private static string RemoveGenericSuffix(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return typeName;

        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex >= 0)
        {
            return typeName.Substring(0, backtickIndex).Trim();
        }

        return typeName.Trim();
    }

    /// <summary>
    /// Match type name to VariableType (handles both simple and fully qualified names)
    /// Also handles generic type suffixes like `1, `2, etc.
    /// </summary>
    private static VariableType MatchTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return VariableType.Object;

        // Remove generic suffix if present (e.g., "List`1" -> "List")
        var nameWithoutSuffix = RemoveGenericSuffix(typeName);
        var lower = nameWithoutSuffix.ToLowerInvariant();

        // Remove namespace prefix if present
        var simpleName = lower.Contains('.') ? lower.Substring(lower.LastIndexOf('.') + 1) : lower;

        return simpleName switch
        {
            "string" => VariableType.String,
            "int" or "int32" => VariableType.Int,
            "double" => VariableType.Double,
            "bool" or "boolean" => VariableType.Bool,
            "datetime" => VariableType.DateTime,
            "object" => VariableType.Object,
            _ => VariableType.Object
        };
    }

    /// <summary>
    /// Check if type name represents List type
    /// Handles: "List", "List`1", "System.Collections.Generic.List", "System.Collections.Generic.List`1"
    /// </summary>
    private static bool IsListType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        // Remove generic suffix if present
        typeName = RemoveGenericSuffix(typeName);

        return typeName.Equals("List", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("System.Collections.Generic.List", StringComparison.OrdinalIgnoreCase) ||
               typeName.EndsWith(".List", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if type name represents Dictionary type
    /// Handles: "Dictionary", "Dictionary`2", "System.Collections.Generic.Dictionary", "System.Collections.Generic.Dictionary`2"
    /// </summary>
    private static bool IsDictionaryType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        // Remove generic suffix if present
        typeName = RemoveGenericSuffix(typeName);

        return typeName.Equals("Dictionary", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("System.Collections.Generic.Dictionary", StringComparison.OrdinalIgnoreCase) ||
               typeName.EndsWith(".Dictionary", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if type name represents string type
    /// Handles: "string", "String", "System.String"
    /// </summary>
    private static bool IsStringType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        // Remove namespace prefix if present
        var simpleName = typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;

        return simpleName.Equals("string", StringComparison.OrdinalIgnoreCase) ||
               simpleName.Equals("String", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("System.String", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if type name represents object type
    /// Handles: "object", "Object", "System.Object"
    /// </summary>
    private static bool IsObjectType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        // Remove namespace prefix if present
        var simpleName = typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;

        return simpleName.Equals("object", StringComparison.OrdinalIgnoreCase) ||
               simpleName.Equals("Object", StringComparison.OrdinalIgnoreCase) ||
               typeName.Equals("System.Object", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Convert value to string representation based on VariableType
    /// This is a generic method that can be used by both .Common and .Quicker projects
    /// </summary>
    public static string ConvertValueToString(this VariableType varType, object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        // Handle JsonElement first (from JSON deserialization)
        if (value is JsonElement jsonElement)
        {
            var convertedValue = varType.ConvertValueFromJson(jsonElement);
            // Recursively convert if still JsonElement, otherwise use ToString
            if (convertedValue is JsonElement nestedElement)
            {
                return nestedElement.ToString();
            }
            // For converted values, use simple ToString for most types
            value = convertedValue;
        }

        return varType switch
        {
            VariableType.Bool => value is bool boolVal ? boolVal.ToString().ToLower() : value.ToString() ?? "false",
            VariableType.ListString => value is System.Collections.IEnumerable enumerable && !(value is string)
                ? string.Join("\n", enumerable.Cast<object>().Select(item => item?.ToString() ?? ""))
                : string.Empty,
            VariableType.Dictionary => value.ToJson(indented: true),
            // For other types, just use ToString()
            _ => value.ToString() ?? string.Empty
        };
    }
}

