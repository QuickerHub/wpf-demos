namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for double type variables
/// </summary>
public class DoubleVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "double";

    public string FormatDefaultValue() => "0.0";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is double || value is float || value is decimal)
        {
            return value.ToString() ?? "0.0";
        }

        return "0.0";
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is double || value is float || value is decimal)
        {
            return value.ToString() ?? "0.0";
        }

        return "0.0";
    }

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return 0.0;
        }

        // If already double, return as-is
        if (value is double doubleValue)
        {
            return doubleValue;
        }

        // If JsonElement, extract double value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return jsonElement.GetDouble();
            }
            return 0.0;
        }

        // Try to parse from string
        if (value is string str && double.TryParse(str, out var parsedDouble))
        {
            return parsedDouble;
        }

        // Try to convert numeric types
        if (value is int intValue)
        {
            return (double)intValue;
        }
        if (value is long longValue)
        {
            return (double)longValue;
        }
        if (value is float floatValue)
        {
            return (double)floatValue;
        }
        if (value is decimal decimalValue)
        {
            return (double)decimalValue;
        }

        return 0.0;
    }
}

