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
}

