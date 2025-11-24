using System;
using System.Windows.Threading;

namespace QuickerExpressionAgent.Desktop.Extensions;

/// <summary>
/// Extension methods for Dispatcher
/// </summary>
public static class DispatcherExtensions
{
    /// <summary>
    /// Safely invoke an action on the dispatcher thread with Render priority
    /// </summary>
    public static void InvokeOnRender(this Dispatcher dispatcher, Action action)
    {
        if (dispatcher == null || action == null)
            return;

        dispatcher.BeginInvoke(action, DispatcherPriority.Render);
    }

    /// <summary>
    /// Safely invoke an action on the dispatcher thread with Loaded priority
    /// </summary>
    public static void InvokeOnLoaded(this Dispatcher dispatcher, Action action)
    {
        if (dispatcher == null || action == null)
            return;

        dispatcher.BeginInvoke(action, DispatcherPriority.Loaded);
    }
}

