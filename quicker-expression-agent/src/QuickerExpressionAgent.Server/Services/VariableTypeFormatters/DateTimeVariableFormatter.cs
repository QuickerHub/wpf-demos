namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for DateTime type variables
/// </summary>
public class DateTimeVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "DateTime";

    public string FormatDefaultValue() => "DateTime.Now";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is DateTime dateTime)
        {
            return $"DateTime.Parse(\"{dateTime:yyyy-MM-dd HH:mm:ss}\")";
        }

        return FormatDefaultValue();
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is DateTime dateTime)
        {
            return $"DateTime.Parse(\"{dateTime:yyyy-MM-dd HH:mm:ss}\")";
        }

        return "null";
    }
}

