using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using log4net;

namespace QuickerExpressionAgent.Quicker;

public enum LauncherStatus
{
    NotStarted,
    Started,
    Stopped
}

/// <summary>
/// Launcher class for initializing and managing the Quicker integration
/// </summary>
public static class Launcher
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(Launcher));
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            // AddDebug outputs to Visual Studio Debug Output window
            logging.AddDebug();
            // Set minimum level to Trace so Debug and Trace logs are output
            logging.SetMinimumLevel(LogLevel.Trace);
        })
        .ConfigureServices((context, services) =>
        {
            services.AddLogging();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<ApplicationLauncher>();
            services.AddSingleton<CodeEditorWrapperManager>();
            services.AddSingleton<QuickerServiceImplementation>();
            // Register as singleton and hosted service
            services.AddSingleton<QuickerServiceServer>();
            services.AddHostedService(s=>s.GetRequiredService<QuickerServiceServer>());
        })
        .Build();

    private static LauncherStatus _status = LauncherStatus.NotStarted;
    private static readonly object _lockObject = new();
    private static MainWindow? _mainWindow;

    /// <summary>
    /// Get a service from the dependency injection container
    /// Automatically starts the launcher if not already started
    /// </summary>
    public static T GetService<T>() where T : class
    {
        Start();
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Start the launcher and initialize services
    /// </summary>
    public static void Start()
    {
        lock (_lockObject)
        {
            if (_status == LauncherStatus.Started)
            {
                _log.Info("Launcher is already started, skipping duplicate start");
                return;
            }

            if (_status == LauncherStatus.Stopped)
            {
                _log.Warn("Launcher has been stopped, cannot start again");
                return;
            }

            try
            {
                // Exit other version programs
                ProgramManager.ExitOtherVersionProgram();

                _host.Start();
                // QuickerServiceServer will start automatically as a hosted service
                _status = LauncherStatus.Started;

                _log.Info("Launcher started successfully");
            }
            catch (Exception e)
            {
                _log.Error("Launcher start failed", e);
                throw;
            }
        }
    }

    /// <summary>
    /// Stop the launcher and all services
    /// </summary>
    public static void Stop()
    {
        lock (_lockObject)
        {
            if (_status != LauncherStatus.Started)
            {
                return;
            }

            try
            {
                // QuickerServiceServer will stop automatically as a hosted service
                _host.StopAsync().GetAwaiter().GetResult();
                _status = LauncherStatus.Stopped;
                _log.Info("Launcher stopped successfully");
            }
            catch (Exception ex)
            {
                _log.Error("Error stopping launcher", ex);
            }
        }
    }

    /// <summary>
    /// Get the configuration service
    /// </summary>
    public static ConfigService ConfigService => GetService<ConfigService>();

    /// <summary>
    /// Exit the launcher and all services
    /// Used for exiting other version programs
    /// </summary>
    public static void Exit()
    {
        Stop();
    }

    /// <summary>
    /// Show main window (UI thread only, singleton)
    /// </summary>
    public static void ShowMainWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // If window exists and is not closed, activate it
            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
                return;
            }

            // Create new window instance
            _mainWindow = new MainWindow();

            // Remove reference when window is closed
            _mainWindow.Closed += (s, e) =>
            {
                _mainWindow = null;
            };

            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();
            _mainWindow.Activate();
        });
    }
}

