using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Wrapper for IHostedService that ensures StartAsync and StopAsync run on UI thread
/// </summary>
public class UiThreadHostedService : IHostedService
{
    private readonly IHostedService _innerService;
    private readonly ILogger<UiThreadHostedService>? _logger;

    public UiThreadHostedService(IHostedService innerService, ILogger<UiThreadHostedService>? logger = null)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // If we're already on UI thread, start directly
        if (Application.Current?.Dispatcher != null && Application.Current.Dispatcher.CheckAccess())
        {
            return _innerService.StartAsync(cancellationToken);
        }

        // Otherwise, invoke on UI thread
        if (Application.Current?.Dispatcher != null)
        {
            return Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await _innerService.StartAsync(cancellationToken);
            }, DispatcherPriority.Normal, cancellationToken).Task.Unwrap();
        }

        // No UI thread available, start on current thread
        _logger?.LogWarning("No UI thread available, starting service on current thread");
        return _innerService.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // If we're already on UI thread, stop directly
        if (Application.Current?.Dispatcher != null && Application.Current.Dispatcher.CheckAccess())
        {
            return _innerService.StopAsync(cancellationToken);
        }

        // Otherwise, invoke on UI thread
        if (Application.Current?.Dispatcher != null)
        {
            return Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await _innerService.StopAsync(cancellationToken);
            }, DispatcherPriority.Normal, cancellationToken).Task.Unwrap();
        }

        // No UI thread available, stop on current thread
        return _innerService.StopAsync(cancellationToken);
    }
}

