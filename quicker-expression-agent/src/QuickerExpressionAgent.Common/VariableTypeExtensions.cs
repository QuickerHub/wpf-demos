using System.Text.Json;

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
            VariableType.ListString => value.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                .Select(line => line.TrimEnd())
                .ToList(),
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

        // Handle generic types
        if (normalized.Contains('<') && normalized.Contains('>'))
        {
            var genericStart = normalized.IndexOf('<');
            var genericName = normalized.Substring(0, genericStart).Trim();
            var genericArgs = normalized.Substring(genericStart + 1, normalized.LastIndexOf('>') - genericStart - 1).Trim();

            if (IsListType(genericName) && IsStringType(genericArgs))
                return VariableType.ListString;

            if (IsDictionaryType(genericName))
            {
                var args = genericArgs.Split(',');
                if (args.Length >= 2 && IsStringType(args[0].Trim()) && IsObjectType(args[1].Trim()))
                    return VariableType.Dictionary;
            }
        }

        // Handle non-generic types
        return MatchTypeName(normalized);
    }

    /// <summary>
    /// Match type name to VariableType (handles both simple and fully qualified names)
    /// </summary>
    private static VariableType MatchTypeName(string typeName)
    {
        var lower = typeName.ToLowerInvariant();
        
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
    /// </summary>
    private static bool IsListType(string typeName) =>
        typeName.Equals("List", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("System.Collections.Generic.List", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if type name represents Dictionary type
    /// </summary>
    private static bool IsDictionaryType(string typeName) =>
        typeName.Equals("Dictionary", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("System.Collections.Generic.Dictionary", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if type name represents string type
    /// </summary>
    private static bool IsStringType(string typeName) =>
        typeName.Equals("string", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("System.String", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if type name represents object type
    /// </summary>
    private static bool IsObjectType(string typeName) =>
        typeName.Equals("object", StringComparison.OrdinalIgnoreCase) ||
        typeName.Equals("System.Object", StringComparison.OrdinalIgnoreCase);
}

