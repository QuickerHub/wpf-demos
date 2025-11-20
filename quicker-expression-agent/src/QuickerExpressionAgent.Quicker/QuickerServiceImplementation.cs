using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Implementation of IQuickerService for the Quicker integration
/// </summary>
[H.IpcGenerators.IpcServer]
public partial class QuickerServiceImplementation : IQuickerService
{
    private readonly ExpressionAgentToolHandlerService _toolHandlerService;

    public QuickerServiceImplementation(ExpressionAgentToolHandlerService toolHandlerService)
    {
        _toolHandlerService = toolHandlerService;
    }

    /// <summary>
    /// Get handler by handler ID
    /// </summary>
    /// <param name="handlerId">Handler ID (returns standalone handler if empty or null)</param>
    /// <returns>Expression agent tool handler</returns>
    private IExpressionAgentToolHandler GetHandler(string handlerId)
    {
        return _toolHandlerService.GetHandler(handlerId);
    }

    /// <summary>
    /// Execute a C# expression
    /// </summary>
    public Task<ExpressionResult> ExecuteExpressionAsync(ExpressionRequest request)
    {
        // Use standalone handler for execution
        return GetHandler("standalone").TestExpression(request.Code, request.VariableList);
    }

    /// <summary>
    /// Set expression code and variable list
    /// </summary>
    public Task SetExpressionAsync(ExpressionRequest request)
    {
        // Use standalone handler for setting expression
        var handler = GetHandler("standalone");
        handler.Expression = request.Code;
        
        // Set variables
        if (request.VariableList != null)
        {
            foreach (var variable in request.VariableList)
            {
                handler.SetVariable(variable);
            }
        }
        
        return Task.CompletedTask;
    }

    #region Tool Handler Methods

    public Task<string> GetCodeWrapperIdAsync(string windowHandle)
    {
        if (string.IsNullOrEmpty(windowHandle) || !long.TryParse(windowHandle, out var handleValue))
        {
            // Return standalone handler ID for empty or invalid handle
            return Task.FromResult("standalone");
        }
        return Task.FromResult(_toolHandlerService.GetHandlerIdByWindowHandle(new IntPtr(handleValue)));
    }

    public Task<ExpressionRequest> GetExpressionAndVariablesForWrapperAsync(string handlerId)
    {
        var handler = GetHandler(handlerId);
        return Task.FromResult(new ExpressionRequest
        {
            Code = handler.Expression,
            VariableList = handler.GetAllVariables()
        });
    }

    public Task SetExpressionForWrapperAsync(string handlerId, string expression)
    {
        GetHandler(handlerId).Expression = expression;
        return Task.CompletedTask;
    }

    public Task<VariableClass?> GetVariableForWrapperAsync(string handlerId, string name)
    {
        return Task.FromResult(GetHandler(handlerId).GetVariable(name));
    }

    public Task SetVariableForWrapperAsync(string handlerId, VariableClass variable)
    {
        GetHandler(handlerId).SetVariable(variable);
        return Task.CompletedTask;
    }

    public Task<ExpressionResult> TestExpressionForWrapperAsync(string handlerId, ExpressionRequest request)
    {
        return GetHandler(handlerId).TestExpression(request.Code, request.VariableList);
    }

    #endregion
}

