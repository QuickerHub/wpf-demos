using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Launcher class for initializing and managing the Quicker integration
/// </summary>
public static class Launcher
{
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddLogging();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<ApplicationLauncher>();
            services.AddSingleton<QuickerServiceImplementation>();
            services.AddHostedService<QuickerServiceServer>();
        })
        .Build();

    private static bool _isStarted = false;
    private static readonly SemaphoreSlim _startLock = new(1, 1);

    /// <summary>
    /// Get a service from the dependency injection container
    /// Automatically starts the launcher if not already started
    /// </summary>
    public static T GetService<T>() where T : class
    {
        StartAsync().Wait();
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Start the launcher and initialize services
    /// </summary>
    public static async Task StartAsync()
    {
        if (_isStarted)
        {
            return;
        }

        await _startLock.WaitAsync();
        try
        {
            if (_isStarted)
            {
                return;
            }

            _host.Start();
            // QuickerServiceServer will start automatically as a hosted service
            _isStarted = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Stop the launcher and all services
    /// </summary>
    public static async Task StopAsync()
    {
        if (!_isStarted)
        {
            return;
        }

        await _startLock.WaitAsync();
        try
        {
            if (!_isStarted)
            {
                return;
            }

            // QuickerServiceServer will stop automatically as a hosted service
            await _host.StopAsync();
            _isStarted = false;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Get the configuration service
    /// </summary>
    public static ConfigService ConfigService => GetService<ConfigService>();
}

