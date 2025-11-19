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
}

