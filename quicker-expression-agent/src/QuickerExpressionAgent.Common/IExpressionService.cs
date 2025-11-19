using H.Pipes;
using MessagePack;

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
[MessagePackObject]
public class VariableClass
{
    [Key(0)]
    public string VarName { get; set; } = string.Empty;
    
    [Key(1)]
    public VariableType VarType { get; set; } = VariableType.String;
    
    [Key(2)]
    public object DefaultValue { get; set; } = new();
}

/// <summary>
/// Service interface for expression execution in Quicker
/// </summary>
public interface IExpressionService
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
}

/// <summary>
/// Expression request (used for both execution and setting)
/// Expression uses {varname} format, which will be replaced with actual variable names during execution
/// </summary>
[MessagePackObject]
public class ExpressionRequest
{
    [Key(0)]
    public string Code { get; set; } = string.Empty;
    
    [Key(1)]
    public List<VariableClass> VariableList { get; set; } = new();
}

/// <summary>
/// Expression execution result
/// </summary>
[MessagePackObject]
public class ExpressionResult
{
    [Key(0)]
    public bool Success { get; set; }
    
    [Key(1)]
    public object Value { get; set; } = new();
    
    [Key(2)]
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Auto-generated IPC client for IExpressionService
/// </summary>
[H.IpcGenerators.IpcClient]
public partial class ExpressionServiceClient : IExpressionService
{
}

/// <summary>
/// Helper class for creating pipe server and client
/// </summary>
public static class PipeHelper
{
    /// <summary>
    /// Create a pipe server with MessagePack formatter
    /// </summary>
    public static PipeServer<T> GetServer<T>(string name)
    {
        return new PipeServer<T>(name, new MessagePackFormatter());
    }

    /// <summary>
    /// Create a pipe client with MessagePack formatter
    /// </summary>
    public static PipeClient<T> GetClient<T>(string name)
    {
        return new PipeClient<T>(name, ".", null, new MessagePackFormatter());
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

