using System;
using System.Collections.Generic;
using H.Pipes;

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
    
    public object DefaultValue { get; set; } = new();
}

/// <summary>
/// Service interface for expression execution in Quicker
/// 
/// NOTE: This interface uses <c>List&lt;VariableClass&gt;</c> instead of arrays or other collection types.
/// This is due to a known limitation/bug in H.Ipc generator which has issues with:
/// - Generic <c>List&lt;T&gt;</c> types in some scenarios
/// - Array types (<c>VariableClass[]</c>) 
/// - <c>IList&lt;T&gt;</c> or other collection interfaces
/// 
/// While <c>List&lt;VariableClass&gt;</c> may cause compilation warnings with H.Ipc generator,
/// it is the most compatible type that works with the current H.Ipc implementation.
/// If you encounter generator errors, consider using arrays or <c>IList&lt;T&gt;</c> as alternatives,
/// but be aware they may also have issues with the H.Ipc generator.
/// </summary>
public interface IQuickerService
{
    /// <summary>
    /// Execute a C# expression in Quicker
    /// Expression uses {varname} format, which will be replaced with actual variable names during execution
    /// </summary>
    /// <param name="request">Expression request</param>
    /// <returns>Expression execution result</returns>
    Task<ExpressionResult> ExecuteExpressionAsync(ExpressionRequest request);

    /// <summary>
    /// Set expression code and variable list directly
    /// </summary>
    /// <param name="request">Expression request</param>
    Task SetExpressionAsync(ExpressionRequest request);
    
    // Tool Handler methods for code editor wrapper operations
    // Wrapper ID is the hash code of the CodeEditorWrapper instance as string
    
    /// <summary>
    /// Get wrapper ID by window handle
    /// Returns empty string if not found
    /// </summary>
    Task<string> GetCodeWrapperIdAsync(string windowHandle);
    
    /// <summary>
    /// Get current expression and all variables from a specific code editor wrapper
    /// </summary>
    Task<ExpressionAndVariables> GetExpressionAndVariablesForWrapperAsync(string wrapperId);
    
    /// <summary>
    /// Set expression for a specific code editor wrapper
    /// </summary>
    Task SetExpressionForWrapperAsync(string wrapperId, string expression);
    
    /// <summary>
    /// Get a variable from a specific code editor wrapper
    /// </summary>
    Task<VariableClass?> GetVariableForWrapperAsync(string wrapperId, string name);
    
    /// <summary>
    /// Set or update a variable for a specific code editor wrapper
    /// </summary>
    Task SetVariableForWrapperAsync(string wrapperId, VariableClass variable);
    
    /// <summary>
    /// Test an expression for a specific code editor wrapper
    /// </summary>
    Task<ExpressionResult> TestExpressionForWrapperAsync(string wrapperId, ExpressionRequest request);
}

/// <summary>
/// Expression request (used for both execution and setting)
/// Expression uses {varname} format, which will be replaced with actual variable names during execution
/// 
/// NOTE: VariableList uses List&lt;VariableClass&gt; despite known H.Ipc generator issues.
/// This is because alternative types (arrays, IList&lt;T&gt;) also have problems with the generator.
/// </summary>
public class ExpressionRequest
{
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// List of variables for the expression.
    /// Uses List&lt;VariableClass&gt; due to H.Ipc generator limitations with other collection types.
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
    /// Uses List&lt;VariableClass&gt; due to H.Ipc generator limitations with other collection types.
    /// </summary>
    public List<VariableClass> UsedVariables { get; set; } = [];
}

/// <summary>
/// Expression and variables information for a code editor wrapper
/// </summary>
public class ExpressionAndVariables
{
    public string Expression { get; set; } = string.Empty;
    
    /// <summary>
    /// List of variables for the code editor wrapper.
    /// Uses List&lt;VariableClass&gt; due to H.Ipc generator limitations with other collection types.
    /// </summary>
    public List<VariableClass> Variables { get; set; } = new();
}

/// <summary>
/// IPC Client generated by H.Ipc
/// </summary>
[H.IpcGenerators.IpcClient]
public partial class QuickerServiceClient : IQuickerService
{
}

/// <summary>
/// Helper class for creating PipeServer and PipeClient
/// </summary>
public static class PipeHelper
{
    public static PipeServer<string> GetServer(string name)
    {
        return new PipeServer<string>(name, new MessagePackFormatter());
    }
    
    public static PipeClient<string> GetClient(string name)
    {
        return new PipeClient<string>(name, ".", null, new MessagePackFormatter());
    }
}

/// <summary>
/// MessagePack formatter for H.Pipes
/// </summary>
public class MessagePackFormatter : H.Formatters.IFormatter
{
    public byte[] Serialize(object? obj)
    {
        return MessagePack.MessagePackSerializer.Serialize(obj);
    }

    public T? Deserialize<T>(byte[]? bytes)
    {
        return MessagePack.MessagePackSerializer.Deserialize<T>(bytes);
    }
}

