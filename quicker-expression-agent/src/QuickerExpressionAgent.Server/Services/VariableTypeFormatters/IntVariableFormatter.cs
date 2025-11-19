namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for int type variables
/// </summary>
public class IntVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "int";

    public string FormatDefaultValue() => "0";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is int || value is long || value is short || value is byte)
        {
            return value.ToString() ?? "0";
        }

        return "0";
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is int || value is long || value is short || value is byte)
        {
            return value.ToString() ?? "0";
        }

        return "0";
    }

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        // If already int, return as-is
        if (value is int intValue)
        {
            return intValue;
        }

        // If JsonElement, extract int value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                if (jsonElement.TryGetInt32(out var intVal))
                {
                    return intVal;
                }
                return (int)jsonElement.GetDouble();
            }
            return 0;
        }

        // Try to parse from string
        if (value is string str && int.TryParse(str, out var parsedInt))
        {
            return parsedInt;
        }

        // Try to convert numeric types
        if (value is long longValue)
        {
            return (int)longValue;
        }
        if (value is short shortValue)
        {
            return (int)shortValue;
        }
        if (value is byte byteValue)
        {
            return (int)byteValue;
        }
        if (value is double doubleValue)
        {
            return (int)doubleValue;
        }
        if (value is float floatValue)
        {
            return (int)floatValue;
        }

        return 0;
    }
}

