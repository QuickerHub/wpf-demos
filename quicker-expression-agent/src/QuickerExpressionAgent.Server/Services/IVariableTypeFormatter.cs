using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Interface for formatting variable values for code generation
/// </summary>
public interface IVariableTypeFormatter
{
    /// <summary>
    /// Get the C# type declaration string
    /// </summary>
    string GetTypeDeclaration();

    /// <summary>
    /// Format default value for code (when value is null)
    /// </summary>
    string FormatDefaultValue();

    /// <summary>
    /// Format a value for code generation
    /// </summary>
    string FormatValue(object? value);

    /// <summary>
    /// Format a value for Dictionary entry (used when value is inside a Dictionary)
    /// </summary>
    string FormatValueForDictionary(object? value);
    
    /// <summary>
    /// Parse an object value to the correct type for this variable type
    /// Handles JsonElement, string, and other object types
    /// </summary>
    object? ParseValue(object? value);
}

