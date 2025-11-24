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

    /// <summary>
    /// Get window handle for a specific handler
    /// Returns IntPtr.Zero if handler is not found or handle is not available
    /// </summary>
    Task<long> GetWindowHandleAsync(string handlerId);
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

    /// <summary>
    /// Original object value (for local use, not serialized over JsonRpc)
    /// This field is ignored during JSON serialization to avoid serialization issues
    /// </summary>
#if NET472
    [Newtonsoft.Json.JsonIgnore]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    public object? Value { get; set; }

    /// <summary>
    /// Value stored as JSON string for reliable serialization over JsonRpc
    /// This field is used when serializing for IPC communication
    /// </summary>
    public string? ValueJson { get; set; }

    /// <summary>
    /// C# type name of the Value (e.g., "List`1[System.String]", "Dictionary`2[System.String,System.Object]")
    /// Used for deserialization to know the correct type
    /// Format: Generic type names use backtick notation (e.g., List`1, Dictionary`2)
    /// </summary>
    public string? ValueType { get; set; }

    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// List of variables used in the expression execution.
    /// </summary>
    public List<VariableClass> UsedVariables { get; set; } = [];

    /// <summary>
    /// Default constructor (for deserialization)
    /// </summary>
    public ExpressionResult()
    {
    }

    /// <summary>
    /// Constructor for successful result with value
    /// </summary>
    /// <param name="value">The result value</param>
    /// <param name="usedVariables">List of variables used in the expression execution</param>
    public ExpressionResult(object? value, List<VariableClass>? usedVariables = null)
    {
        Success = true;
        Error = string.Empty;
        UsedVariables = usedVariables ?? [];

        if (value == null)
        {
            Value = null;
            ValueJson = null;
            ValueType = null;
        }
        else
        {
            // Store original object for local use
            Value = value;
            // Serialize to JSON string for IPC communication
            ValueJson = value.ToJson(indented: false);
            // Use GetType().ToString() to get the type name directly (e.g., "System.Collections.Generic.List`1[System.String]")
            ValueType = value.GetType().ToString();
        }
    }

    /// <summary>
    /// Get the value (returns Value if available, otherwise deserializes from ValueJson)
    /// </summary>
    public T? GetValue<T>()
    {
        // If Value is available and matches the requested type, return it directly
        if (Value != null && Value is T typedValue)
        {
            return typedValue;
        }

        // Otherwise, deserialize from JSON string
        if (string.IsNullOrWhiteSpace(ValueJson))
        {
            return default;
        }
        return ValueJson.FromJson<T>();
    }
}

/// <summary>
/// Expression execution error result
/// </summary>
public class ExpressionResultError : ExpressionResult
{
    /// <summary>
    /// Constructor for failed result with error message
    /// </summary>
    /// <param name="error">The error message</param>
    /// <param name="usedVariables">List of variables used in the expression execution</param>
    public ExpressionResultError(string error, List<VariableClass>? usedVariables = null)
    {
        Success = false;
        Error = FilterErrorMessage(error ?? string.Empty);
        Value = null;
        ValueJson = null;
        ValueType = null;
        UsedVariables = usedVariables ?? [];
    }

    /// <summary>
    /// Filter error message to remove support contact information
    /// </summary>
    private static string FilterErrorMessage(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return errorMessage;

        // Remove support contact information
        const string supportInfo = "Oops! The type could not be found. Contact our support team for more information or if you believe it's an error on our part: info@zzzprojects.com.";
#if NET8_0_OR_GREATER
        var filtered = errorMessage.Replace(supportInfo, string.Empty, StringComparison.OrdinalIgnoreCase);
#elif NET472_OR_GREATER
        var filtered = errorMessage.Replace(supportInfo, string.Empty);
#endif
        // Clean up any extra whitespace or newlines
        return filtered.Trim();
    }
}



