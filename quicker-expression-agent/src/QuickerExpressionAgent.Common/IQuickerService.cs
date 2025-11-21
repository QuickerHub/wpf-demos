using System;
using System.Collections.Generic;

namespace QuickerExpressionAgent.Common;

/// <summary>
/// Supported variable types in Quicker
/// </summary>
public enum VariableType
{
    String,
    Int,
    Double,
    Bool,
    DateTime,
    ListString,  // List<string>
    Dictionary,  // Dictionary<string, object>
    Object
}

/// <summary>
/// Variable information class
/// </summary>
public class VariableClass
{
    public string VarName { get; set; } = string.Empty;
    
    public VariableType VarType { get; set; } = VariableType.String;
    
    /// <summary>
    /// Default value stored as JSON string
    /// Use GetDefaultValue() to get the deserialized object value
    /// Use SetDefaultValue(object) to set the value (will be serialized automatically)
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Get the deserialized default value as object
    /// </summary>
    public object? GetDefaultValue()
    {
        if (string.IsNullOrEmpty(DefaultValue))
        {
            return VarType.GetDefaultValue();
        }
        return VarType.ConvertValueFromString(DefaultValue);
    }

    /// <summary>
    /// Set the default value from an object (will be serialized to string)
    /// </summary>
    public void SetDefaultValue(object? value)
    {
        if (value == null)
        {
            DefaultValue = string.Empty;
        }
        else
        {
            DefaultValue = VarType.ConvertValueToString(value);
        }
    }
}

/// <summary>
/// Service interface for expression execution in Quicker
/// </summary>
public interface IQuickerService
{
    // Tool Handler methods for code editor wrapper operations
    // Handler ID is the hash code of the CodeEditorWrapper instance as string, or "standalone" for standalone handler
    
    /// <summary>
    /// Get handler ID by window handle
    /// Returns "standalone" if handle is empty or invalid
    /// </summary>
    Task<string> GetCodeWrapperIdAsync(string windowHandle);
    
    /// <summary>
    /// Get or create a Code Editor window and return its handler ID
    /// If an active Code Editor window exists, returns its handler ID
    /// Otherwise, creates a new Code Editor window, shows it, and returns its handler ID
    /// </summary>
    Task<string> GetOrCreateCodeEditorAsync();
    
    /// <summary>
    /// Get current expression and all variables from a specific handler
    /// </summary>
    Task<ExpressionRequest> GetExpressionAndVariablesForWrapperAsync(string handlerId);
    
    /// <summary>
    /// Set expression for a specific handler
    /// </summary>
    Task SetExpressionForWrapperAsync(string handlerId, string expression);
    
    /// <summary>
    /// Get a variable from a specific handler
    /// </summary>
    Task<VariableClass?> GetVariableForWrapperAsync(string handlerId, string name);
    
    /// <summary>
    /// Set or update a variable for a specific handler
    /// </summary>
    Task SetVariableForWrapperAsync(string handlerId, VariableClass variable);
    
    /// <summary>
    /// Test an expression for a specific handler
    /// </summary>
    Task<ExpressionResult> TestExpressionForWrapperAsync(string handlerId, ExpressionRequest request);
}

/// <summary>
/// Expression request (used for both execution and setting)
/// Expression uses {varname} format, which will be replaced with actual variable names during execution
/// 
/// </summary>
public class ExpressionRequest
{
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// List of variables for the expression.
    /// </summary>
    public List<VariableClass> VariableList { get; set; } = new();
}

/// <summary>
/// Expression execution result
/// </summary>
public class ExpressionResult
{
    public bool Success { get; set; }
    
    public object Value { get; set; } = new();
    
    public string Error { get; set; } = string.Empty;
    
    /// <summary>
    /// List of variables used in the expression execution.
    /// </summary>
    public List<VariableClass> UsedVariables { get; set; } = [];
}



