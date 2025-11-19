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
}

