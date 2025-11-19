using System;

namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for string type variables
/// </summary>
public class StringVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "string";

    public string FormatDefaultValue() => "\"\"";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        if (value is string str)
        {
            // Check if it's a code expression
            var trimmed = str.Trim();
            if (IsCodeExpression(trimmed))
            {
                return trimmed;
            }
            
            // It's a string literal, add quotes
            return $"\"{str.Replace("\"", "\\\"")}\"";
        }

        // Convert to string
        return $"\"{value.ToString()?.Replace("\"", "\\\"") ?? ""}\"";
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string str)
        {
            return $"\"{str.Replace("\"", "\\\"")}\"";
        }

        return $"\"{value.ToString()?.Replace("\"", "\\\"") ?? ""}\"";
    }

    private bool IsCodeExpression(string trimmed)
    {
        // Check for object creation
        if (trimmed.StartsWith("new ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for static member access
        if (trimmed.Contains(".") && !trimmed.StartsWith("\"") && !trimmed.EndsWith("\""))
        {
            var parts = trimmed.Split('.');
            if (parts.Length >= 2)
            {
                var firstPart = parts[0].Trim();
                if (firstPart.Length > 0 && char.IsUpper(firstPart[0]))
                {
                    return true;
                }
            }
        }

        // Check for method calls
        if (trimmed.Contains("(") && trimmed.Contains(")") &&
            !trimmed.StartsWith("\"") && !trimmed.EndsWith("\""))
        {
            return true;
        }

        return false;
    }

    public object? ParseValue(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        // If already string, return as-is
        if (value is string str)
        {
            return str;
        }

        // If JsonElement, extract string value
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return jsonElement.GetString() ?? string.Empty;
            }
            return jsonElement.ToString();
        }

        // Convert to string
        return value.ToString() ?? string.Empty;
    }
}

