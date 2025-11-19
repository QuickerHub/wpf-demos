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

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return default(DateTime);
        }

        // If already DateTime, return as-is
        if (value is DateTime dateTime)
        {
            return dateTime;
        }

        // If JsonElement, extract DateTime value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var jsonStr = jsonElement.GetString();
                if (!string.IsNullOrEmpty(jsonStr) && DateTime.TryParse(jsonStr, out var jsonDateTime))
                {
                    return jsonDateTime;
                }
            }
            return default(DateTime);
        }

        // Try to parse from string
        if (value is string strValue && DateTime.TryParse(strValue, out var stringDateTime))
        {
            return stringDateTime;
        }

        return default(DateTime);
    }
}

