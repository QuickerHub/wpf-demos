using System;
using log4net;
using Microsoft.Extensions.Logging;

namespace QuickerExpressionAgent.Quicker.Logging;

/// <summary>
/// Log4Net logger provider for Microsoft.Extensions.Logging
/// Forwards all logs to log4net LogManager
/// </summary>
public class Log4NetLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new Log4NetLogger(LogManager.GetLogger(categoryName));
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Log4Net logger implementation for Microsoft.Extensions.Logging
/// </summary>
public class Log4NetLogger : ILogger
{
    private readonly ILog _log;

    public Log4NetLogger(ILog log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => _log.IsDebugEnabled,
            LogLevel.Debug => _log.IsDebugEnabled,
            LogLevel.Information => _log.IsInfoEnabled,
            LogLevel.Warning => _log.IsWarnEnabled,
            LogLevel.Error => _log.IsErrorEnabled,
            LogLevel.Critical => _log.IsFatalEnabled,
            LogLevel.None => false,
            _ => false
        };
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                _log.Debug(message, exception);
                break;
            case LogLevel.Information:
                _log.Info(message, exception);
                break;
            case LogLevel.Warning:
                _log.Warn(message, exception);
                break;
            case LogLevel.Error:
                _log.Error(message, exception);
                break;
            case LogLevel.Critical:
                _log.Fatal(message, exception);
                break;
        }
    }
}

