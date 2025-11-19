namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for bool type variables
/// </summary>
public class BoolVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "bool";

    public string FormatDefaultValue() => "false";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        return "false";
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        return "false";
    }

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return false;
        }

        // If already bool, return as-is
        if (value is bool boolValue)
        {
            return boolValue;
        }

        // If JsonElement, extract bool value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.True)
            {
                return true;
            }
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.False)
            {
                return false;
            }
            return false;
        }

        // Try to parse from string
        if (value is string str && bool.TryParse(str, out var parsedBool))
        {
            return parsedBool;
        }

        return false;
    }
}

