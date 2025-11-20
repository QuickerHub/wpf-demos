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
/// Manager for CodeEditorWrapper instances, identified by wrapper hash code
/// </summary>
public class CodeEditorWrapperManager
{
    private readonly ConcurrentDictionary<string, CodeEditorWrapper> _wrappers = new ConcurrentDictionary<string, CodeEditorWrapper>();

    /// <summary>
    /// Register a code editor wrapper
    /// </summary>
    public void Register(CodeEditorWrapper wrapper)
    {
        if (wrapper == null)
        {
            throw new ArgumentNullException(nameof(wrapper));
        }

        var wrapperId = wrapper.WrapperId;
        _wrappers[wrapperId] = wrapper;
    }

    /// <summary>
    /// Unregister a code editor wrapper
    /// </summary>
    public void Unregister(string wrapperId)
    {
        _wrappers.TryRemove(wrapperId, out _);
    }

    /// <summary>
    /// Unregister a code editor wrapper by instance
    /// </summary>
    public void Unregister(CodeEditorWrapper wrapper)
    {
        if (wrapper == null)
        {
            return;
        }

        Unregister(wrapper.WrapperId);
    }

    /// <summary>
    /// Get a code editor wrapper by wrapper ID (hash code as string)
    /// </summary>
    public CodeEditorWrapper? GetWrapper(string wrapperId)
    {
        return _wrappers.TryGetValue(wrapperId, out var wrapper) ? wrapper : null;
    }

    /// <summary>
    /// Create a wrapper for the given window handle if it's a code editor window
    /// </summary>
    public CodeEditorWrapper? CreateWrapper(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }

        // Check if the window is a CodeEditorWindow
        var window = GetWindowFromHandle(windowHandle);
        if (window is CodeEditorWindow codeEditorWindow)
        {
            // Check if wrapper already exists
            foreach (var existingWrapper in _wrappers.Values)
            {
                if (existingWrapper.WindowHandle == windowHandle)
                {
                    return existingWrapper;
                }
            }

            // Create new wrapper with existing window
            // Note: CodeEditorWrapper currently only has a parameterless constructor
            // This might need to be refactored to accept an existing CodeEditorWindow
            var wrapper = new CodeEditorWrapper();
            Register(wrapper);
            return wrapper;
        }

        return null;
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
    /// Get all registered wrapper IDs
    /// </summary>
    public List<string> GetAllWrapperIds()
    {
        return _wrappers.Keys.ToList();
    }

    /// <summary>
    /// Get all registered wrappers
    /// </summary>
    public List<CodeEditorWrapper> GetAllWrappers()
    {
        return _wrappers.Values.ToList();
    }

    /// <summary>
    /// Find wrapper ID by window handle
    /// Returns empty string if not found
    /// </summary>
    public string GetWrapperIdByWindowHandle(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return string.Empty;
        }

        foreach (var wrapper in _wrappers.Values)
        {
            try
            {
                var wrapperHandle = wrapper.WindowHandle;
                if (wrapperHandle == windowHandle)
                {
                    return wrapper.WrapperId;
                }
            }
            catch
            {
                // Ignore errors when accessing window handle
                continue;
            }
        }

        return string.Empty;
    }
}

