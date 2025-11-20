using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Quicker.View;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Service for managing expression agent tool handlers
/// Identified by wrapper hash code
/// </summary>
public class ExpressionAgentToolHandlerService
{
    private readonly ConcurrentDictionary<string, IExpressionAgentToolHandler> _handlers = new();
    private readonly StandaloneExpressionToolHandler _standaloneHandler;
    private const string StandaloneHandlerId = "standalone";

    public ExpressionAgentToolHandlerService()
    {
        _standaloneHandler = new StandaloneExpressionToolHandler();
        _handlers[StandaloneHandlerId] = _standaloneHandler;
    }

    /// <summary>
    /// Register a tool handler
    /// </summary>
    public void Register(IExpressionAgentToolHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var wrapperId = handler is CodeEditorWrapper codeWrapper ? codeWrapper.WrapperId : StandaloneHandlerId;
        _handlers[wrapperId] = handler;
    }

    /// <summary>
    /// Unregister a tool handler
    /// </summary>
    public void Unregister(string wrapperId)
    {
        // Don't allow unregistering standalone handler
        if (wrapperId == StandaloneHandlerId)
        {
            return;
        }
        _handlers.TryRemove(wrapperId, out _);
    }

    /// <summary>
    /// Unregister a tool handler by instance
    /// </summary>
    public void Unregister(IExpressionAgentToolHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        if (handler == _standaloneHandler)
        {
            return; // Don't unregister standalone handler
        }

        var wrapperId = handler is CodeEditorWrapper codeWrapper ? codeWrapper.WrapperId : null;
        if (wrapperId != null)
        {
            Unregister(wrapperId);
        }
    }

    /// <summary>
    /// Get a tool handler by wrapper ID (hash code as string)
    /// Returns standalone handler if ID is empty or not found
    /// </summary>
    public IExpressionAgentToolHandler GetHandler(string wrapperId)
    {
        if (string.IsNullOrEmpty(wrapperId))
        {
            return _standaloneHandler;
        }

        return _handlers.TryGetValue(wrapperId, out var handler) ? handler : _standaloneHandler;
    }

    /// <summary>
    /// Create a wrapper for the given window handle if it's a code editor window
    /// Returns standalone handler if handle is zero or empty
    /// </summary>
    public IExpressionAgentToolHandler GetOrCreateHandler(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return _standaloneHandler;
        }

        // Check if the window is a CodeEditorWindow
        var window = GetWindowFromHandle(windowHandle);
        if (window is CodeEditorWindow codeEditorWindow)
        {
            // Check if wrapper already exists
            foreach (var existingHandler in _handlers.Values)
            {
                if (existingHandler is CodeEditorWrapper codeWrapper && codeWrapper.WindowHandle == windowHandle)
                {
                    return codeWrapper;
                }
            }

            // Create new wrapper with existing window
            var wrapper = new CodeEditorWrapper();
            Register(wrapper);
            return wrapper;
        }

        return _standaloneHandler;
    }

    /// <summary>
    /// Get window from handle
    /// </summary>
    private Window? GetWindowFromHandle(IntPtr handle)
    {
        try
        {
            var source = HwndSource.FromHwnd(handle);
            return source?.RootVisual as Window;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Find handler ID by window handle
    /// Returns standalone handler ID if handle is zero or not found
    /// </summary>
    public string GetHandlerIdByWindowHandle(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return StandaloneHandlerId;
        }

        foreach (var handler in _handlers.Values)
        {
            if (handler is CodeEditorWrapper codeWrapper)
            {
                try
                {
                    var wrapperHandle = codeWrapper.WindowHandle;
                    if (wrapperHandle == windowHandle)
                    {
                        return codeWrapper.WrapperId;
                    }
                }
                catch
                {
                    // Ignore errors when accessing window handle
                    continue;
                }
            }
        }

        return StandaloneHandlerId;
    }
}

