using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// IExpressionAgentToolHandler implementation for Quicker Code Editor
/// Forwards all calls to Quicker process via IPC
/// </summary>
public class QuickerCodeEditorToolHandler : IExpressionAgentToolHandler
{
    private readonly IntPtr _windowHandle;
    private readonly QuickerServerClientConnector _connector;
    private readonly string _handlerId;

    private IQuickerService Service => _connector.ServiceClient;

    public QuickerCodeEditorToolHandler(IntPtr windowHandle, QuickerServerClientConnector connector)
    {
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("Window handle cannot be zero", nameof(windowHandle));
        }
        
        _windowHandle = windowHandle;
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        
        // Initialize handler ID in constructor
        _handlerId = Service.GetCodeWrapperIdAsync(_windowHandle.ToInt64().ToString()).Result;
    }

    /// <summary>
    /// Current expression code (C# code with {variableName} format)
    /// </summary>
    public string Expression
    {
        get => Service.GetExpressionAndVariablesForWrapperAsync(_handlerId).Result.Code ?? string.Empty;
        set => Service.SetExpressionForWrapperAsync(_handlerId, value).Wait();
    }

    /// <summary>
    /// Set or update a variable
    /// </summary>
    public void SetVariable(VariableClass variable)
    {
        if (variable == null) throw new ArgumentNullException(nameof(variable));
        Service.SetVariableForWrapperAsync(_handlerId, variable).Wait();
    }

    /// <summary>
    /// Get a specific variable by name
    /// </summary>
    public VariableClass? GetVariable(string name) => Service.GetVariableForWrapperAsync(_handlerId, name).Result;

    /// <summary>
    /// Get all variables
    /// </summary>
    public List<VariableClass> GetAllVariables() => Service.GetExpressionAndVariablesForWrapperAsync(_handlerId).Result.VariableList ?? new List<VariableClass>();

    /// <summary>
    /// Test an expression for syntax and execution
    /// </summary>
    public async Task<ExpressionResult> TestExpression(string expression, List<VariableClass>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new ExpressionResult { Success = false, Error = "Expression cannot be empty." };
        
        var request = new ExpressionRequest
        {
            Code = expression,
            VariableList = variables ?? new List<VariableClass>()
        };
        return await Service.TestExpressionForWrapperAsync(_handlerId, request);
    }

    /// <summary>
    /// Get the window handle
    /// </summary>
    public IntPtr WindowHandle => _windowHandle;
}

