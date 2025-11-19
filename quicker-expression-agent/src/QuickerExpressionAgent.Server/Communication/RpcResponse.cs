using System.Text.Json;

namespace QuickerExpressionAgent.Server.Communication;

/// <summary>
/// RPC response message
/// </summary>
public class RpcResponse
{
    public int Id { get; set; }
    public JsonElement? Result { get; set; }
    public RpcError? Error { get; set; }
}

/// <summary>
/// RPC error information
/// </summary>
public class RpcError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

