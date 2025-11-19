using System;

namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Formatter for object type variables
/// </summary>
public class ObjectVariableFormatter : IVariableTypeFormatter
{
    public string GetTypeDeclaration() => "object";

    public string FormatDefaultValue() => "null";

    public string FormatValue(object? value)
    {
        if (value == null)
        {
            return FormatDefaultValue();
        }

        // Try to serialize to string representation
        var strValue = value.ToString() ?? "null";
        
        // Check if it's a code expression
        var trimmed = strValue.Trim();
        if (IsCodeExpression(trimmed))
        {
            return trimmed;
        }
        
        // Default: wrap in quotes as string literal
        return $"\"{strValue.Replace("\"", "\\\"")}\"";
    }

    public string FormatValueForDictionary(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        // Convert to string and wrap in quotes
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
}

