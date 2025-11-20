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
    private readonly CodeEditorWrapperManager _wrapperManager;

    public QuickerServiceImplementation(CodeEditorWrapperManager wrapperManager)
    {
        _wrapperManager = wrapperManager;
    }

    /// <summary>
    /// Execute a C# expression
    /// </summary>
    public Task<ExpressionResult> ExecuteExpressionAsync(ExpressionRequest request)
    {
        // For backward compatibility, use the first available wrapper or create a default one
        var wrappers = _wrapperManager.GetAllWrappers();
        if (wrappers.Count > 0)
        {
            return wrappers[0].TestExpression(request.Code, request.VariableList);
        }
        
        // Create a temporary wrapper for testing
        var tempWrapper = new CodeEditorWrapper();
        return tempWrapper.TestExpression(request.Code, request.VariableList);
    }

    /// <summary>
    /// Set expression code and variable list
    /// </summary>
    public Task SetExpressionAsync(ExpressionRequest request)
    {
        // For backward compatibility, use the first available wrapper or create a new one
        var wrappers = _wrapperManager.GetAllWrappers();
        CodeEditorWrapper wrapper;
        
        if (wrappers.Count > 0)
        {
            wrapper = wrappers[0];
        }
        else
        {
            // Create a new wrapper and register it
            wrapper = new CodeEditorWrapper();
            _wrapperManager.Register(wrapper);
            wrapper.Show();
        }
        
        // Set expression
        wrapper.Expression = request.Code;
        
        // Set variables
        if (request.VariableList != null)
        {
            foreach (var variable in request.VariableList)
            {
                wrapper.SetVariable(variable);
            }
        }
        
        // Show the code editor window
        wrapper.Show();
        
        return Task.CompletedTask;
    }

    #region Tool Handler Methods

    public Task<string> GetCodeWrapperIdAsync(string windowHandle)
    {
        if (string.IsNullOrEmpty(windowHandle) || !long.TryParse(windowHandle, out var handleValue))
        {
            return Task.FromResult(string.Empty);
        }
        var handle = new IntPtr(handleValue);
        var wrapperId = _wrapperManager.GetWrapperIdByWindowHandle(handle);
        return Task.FromResult(wrapperId);
    }

    public Task<ExpressionAndVariables> GetExpressionAndVariablesForWrapperAsync(string wrapperId)
    {
        var wrapper = _wrapperManager.GetWrapper(wrapperId);
        if (wrapper == null)
        {
            return Task.FromResult(new ExpressionAndVariables
            {
                Expression = string.Empty,
                Variables = new List<VariableClass>()
            });
        }
        return Task.FromResult(new ExpressionAndVariables
        {
            Expression = wrapper.Expression,
            Variables = wrapper.GetAllVariables()
        });
    }

    public Task SetExpressionForWrapperAsync(string wrapperId, string expression)
    {
        var wrapper = _wrapperManager.GetWrapper(wrapperId);
        if (wrapper != null)
        {
            wrapper.Expression = expression;
        }
        return Task.CompletedTask;
    }

    public Task<VariableClass?> GetVariableForWrapperAsync(string wrapperId, string name)
    {
        var wrapper = _wrapperManager.GetWrapper(wrapperId);
        return Task.FromResult(wrapper?.GetVariable(name));
    }

    public Task SetVariableForWrapperAsync(string wrapperId, VariableClass variable)
    {
        var wrapper = _wrapperManager.GetWrapper(wrapperId);
        if (wrapper != null)
        {
            wrapper.SetVariable(variable);
        }
        return Task.CompletedTask;
    }

    public Task<ExpressionResult> TestExpressionForWrapperAsync(string wrapperId, ExpressionRequest request)
    {
        var wrapper = _wrapperManager.GetWrapper(wrapperId);
        if (wrapper == null)
        {
            return Task.FromResult(new ExpressionResult
            {
                Success = false,
                Error = $"Code editor wrapper with ID {wrapperId} not found"
            });
        }
        return wrapper.TestExpression(request.Code, request.VariableList);
    }

    #endregion
}

