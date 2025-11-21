using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Quicker.View;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Implementation of IQuickerService for the Quicker integration
/// </summary>
public class QuickerServiceImplementation : IQuickerService
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
    /// Execute action on UI thread via Task.Run to avoid deadlock
    /// </summary>
    private Task<T> RunOnUIThreadAsync<T>(Func<T> func)
    {
        return Task.Run(() => Application.Current.Dispatcher.Invoke(func));
    }

    /// <summary>
    /// Execute action on UI thread via Task.Run to avoid deadlock
    /// </summary>
    private Task RunOnUIThreadAsync(Action action)
    {
        return Task.Run(() => Application.Current.Dispatcher.Invoke(action));
    }

    #region Tool Handler Methods

    public Task<string> GetCodeWrapperIdAsync(string windowHandle)
    {
        return RunOnUIThreadAsync(() =>
        {
            if (string.IsNullOrEmpty(windowHandle) || !long.TryParse(windowHandle, out var handleValue))
            {
                return "standalone";
            }
            return _toolHandlerService.GetHandlerIdByWindowHandle(new IntPtr(handleValue));
        });
    }

    public Task<string> GetOrCreateCodeEditorAsync()
    {
        return RunOnUIThreadAsync(() => GetOrCreateCodeEditorInternal());
    }

    private string GetOrCreateCodeEditorInternal()
    {
        // Try to find an existing active CodeEditorWindow
        CodeEditorWindow? existingWindow = null;
        foreach (Window window in Application.Current.Windows)
        {
            if (window is CodeEditorWindow codeEditorWindow && window.IsLoaded)
            {
                existingWindow = codeEditorWindow;
                break;
            }
        }

        CodeEditorExpressionToolHandler wrapper;

        if (existingWindow != null)
        {
            // Get window handle from existing window
            var windowHandle = new WindowInteropHelper(existingWindow).Handle;

            // Try to get existing handler by window handle
            var handlerId = _toolHandlerService.GetHandlerIdByWindowHandle(windowHandle);
            var existingHandler = _toolHandlerService.GetHandler(handlerId);

            if (existingHandler is CodeEditorExpressionToolHandler existingWrapper)
            {
                // Use existing wrapper
                wrapper = existingWrapper;
            }
            else
            {
                // Create new wrapper for existing window
                wrapper = new CodeEditorExpressionToolHandler(existingWindow);
                wrapper.Show();
                _toolHandlerService.Register(wrapper);
            }
        }
        else
        {
            // Create new CodeEditorWrapper
            wrapper = new CodeEditorExpressionToolHandler();
            wrapper.Show();
            _toolHandlerService.Register(wrapper);
        }

        return wrapper.WrapperId;
    }

    public Task<ExpressionRequest> GetExpressionAndVariablesForWrapperAsync(string handlerId)
    {
        return RunOnUIThreadAsync(() =>
        {
            var handler = GetHandler(handlerId);
            return new ExpressionRequest
            {
                Code = handler.Expression,
                VariableList = handler.GetAllVariables()
            };
        });
    }

    public Task SetExpressionForWrapperAsync(string handlerId, string expression)
    {
        return RunOnUIThreadAsync(() =>
        {
            GetHandler(handlerId).Expression = expression;
        });
    }

    public Task<VariableClass?> GetVariableForWrapperAsync(string handlerId, string name)
    {
        return RunOnUIThreadAsync(() => GetHandler(handlerId).GetVariable(name));
    }

    public Task SetVariableForWrapperAsync(string handlerId, VariableClass variable)
    {
        return RunOnUIThreadAsync(() =>
        {
            GetHandler(handlerId).SetVariable(variable);
        });
    }

    public Task<ExpressionResult> TestExpressionForWrapperAsync(string handlerId, ExpressionRequest request)
    {
        return GetHandler(handlerId).TestExpressionAsync(request.Code, request.VariableList);
    }

    #endregion
}

