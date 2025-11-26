using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Quicker.Extensions;
using QuickerExpressionAgent.Quicker.Logging;
using QuickerExpressionAgent.Quicker.Services;
using log4net;
using Microsoft.VisualStudio.Threading;

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
            // Add Log4Net provider to forward all logs to log4net LogManager
            // This will output logs to the injected process's log file (Quicker's log file)
            logging.AddProvider(new Log4NetLoggerProvider());
            // Set minimum level to Trace so all logs are output
            logging.SetMinimumLevel(LogLevel.Trace);
        })
        .ConfigureServices((context, services) =>
        {
            services.AddLogging();
            services.AddSingleton<ConfigService>();

            // Desktop application configuration
            services.AddSingleton<Services.DesktopAppConfig>();

            // .NET version checker
            services.AddSingleton<Services.DotNetVersionChecker>();

            services.AddSingleton<ApplicationLauncher>();
            services.AddSingleton<ExpressionAgentToolHandlerService>();
            services.AddSingleton<DesktopProcessManager>();
            services.AddSingleton<Services.DesktopStartupService>();

            // Communication services (Desktop <-> Quicker)
            services.AddCommunicationServices();

            // Register MainWindow and ViewModel
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            // Register Test Window and ViewModel
            services.AddTransient<ViewModels.QuickerServiceTestViewModel>();
            services.AddTransient<QuickerServiceTestWindow>();
        })
        .Build();

    private static LauncherStatus _status = LauncherStatus.NotStarted;
    private static readonly object _lockObject = new();
    private static MainWindow? _mainWindow;

    /// <summary>
    /// Get the current launcher status
    /// </summary>
    public static LauncherStatus Status
    {
        get
        {
            lock (_lockObject)
            {
                return _status;
            }
        }
    }

    /// <summary>
    /// Get a service from the dependency injection container
    /// Automatically starts the launcher if not already started
    /// </summary>
    public static T GetService<T>() where T : class
    {
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
                throw new InvalidOperationException("Launcher has been stopped and cannot be started again");
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
                // Close all windows created by current assembly
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var currentAssembly = Assembly.GetExecutingAssembly();
                        var windowsToClose = Application.Current.Windows.Cast<Window>()
                            .Where(w => w.GetType().Assembly == currentAssembly && w.IsLoaded)
                            .ToList();

                        foreach (var window in windowsToClose)
                        {
                            try
                            {
                                window.Close();
                            }
                            catch (Exception ex)
                            {
                                _log.Warn($"Error closing window {window.GetType().Name}", ex);
                            }
                        }

                        // Clear main window reference
                        _mainWindow = null;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Error closing windows", ex);
                    }
                });

                // QuickerServiceServer will stop automatically as a hosted service
                _ = Task.Run(async () =>
                {
                    await _host.StopAsync();
                });
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

        Start();
        // Use BeginInvoke to avoid blocking the caller
        // Don't wait for Start() to complete - show window immediately
        _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
            // If window exists and is not closed, activate it
            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
                return;
            }

            // Create new window instance from DI container
            _mainWindow = GetService<MainWindow>();

            // Remove reference when window is closed
            _mainWindow.Closed += (s, e) =>
            {
                _mainWindow = null;
            };

            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Show();
            _mainWindow.Activate();
        }));
    }
}

