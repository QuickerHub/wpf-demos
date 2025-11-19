using System.Text.Json;

namespace QuickerExpressionAgent.Server.Communication;

/// <summary>
/// RPC request message
/// </summary>
public class RpcRequest
{
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public JsonElement Params { get; set; }
}

