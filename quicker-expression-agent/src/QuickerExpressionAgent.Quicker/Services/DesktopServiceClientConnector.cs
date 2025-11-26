using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Connector for the .Quicker project to connect to the Desktop service via named pipe using StreamJsonRpc
/// This is used by the .Quicker project to call methods implemented in the .Desktop project
/// Automatically connects and reconnects in background via IHostedService
/// Uses StreamJsonRpc for JSON-RPC 2.0 protocol communication
/// </summary>
public class DesktopServiceClientConnector : IHostedService
{
    private NamedPipeClientStream? _pipeStream;
    private JsonRpc? _jsonRpc;
    private IDesktopService? _service;
    private readonly ILogger<DesktopServiceClientConnector> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isConnected;

    public event EventHandler<bool>? ConnectionStatusChanged;

    public DesktopServiceClientConnector(ILogger<DesktopServiceClientConnector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cancellationTokenSource = new CancellationTokenSource();
        _isConnected = false;
    }

    /// <summary>
    /// Get the Desktop service client
    /// </summary>
    public IDesktopService ServiceClient
    {
        get
        {
            if (_service == null)
            {
                throw new InvalidOperationException("Service client is not connected. Wait for connection first.");
            }
            return _service;
        }
    }

    /// <summary>
    /// Check if connected to the Desktop service
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                ConnectionStatusChanged?.Invoke(this, value);
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start connection in background
        _ = Task.Run(async () =>
        {
            try
            {
                await MaintainConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start desktop service client connector");
            }
        }, _cancellationTokenSource.Token);
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        
        _jsonRpc?.Dispose();
        _pipeStream?.Dispose();
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Wait for connection to be established
    /// </summary>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>True if connected, false if timeout</returns>
    public async Task<bool> WaitConnectAsync(TimeSpan timeout)
    {
        if (IsConnected)
        {
            return true;
        }

        var startTime = DateTime.UtcNow;
        while (!IsConnected && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(50);
        }

        return IsConnected;
    }

    /// <summary>
    /// Maintain connection to the Desktop service in a loop
    /// Automatically connects and reconnects on disconnection
    /// </summary>
    private async Task MaintainConnectionAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (_isConnected && _service != null)
            {
                await Task.Delay(1000, _cancellationTokenSource.Token);
                continue;
            }

            try
            {
                // Dispose existing connection if any
                _jsonRpc?.Dispose();
                _pipeStream?.Dispose();

                // Create new pipe client
                _pipeStream = new NamedPipeClientStream(
                    ".",
                    Constants.DesktopServerName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                using var cts = new CancellationTokenSource(5000);
                await _pipeStream.ConnectAsync(cts.Token);

                // Create JsonRpc - it will automatically respect [JsonIgnore] attributes
                // StreamJsonRpc uses System.Text.Json which respects [System.Text.Json.Serialization.JsonIgnore]
                _jsonRpc = new JsonRpc(_pipeStream, _pipeStream);
                _service = _jsonRpc.Attach<IDesktopService>();
                _jsonRpc.StartListening();

                _logger.LogInformation("Connected to desktop service");
                IsConnected = true;

                // Wait for disconnection
                await _jsonRpc.Completion;
            }
            catch (OperationCanceledException)
            {
                // Connection timeout - don't log, just retry silently
                // Only log when connection state actually changes
            }
            catch (Exception ex)
            {
                // Only log non-timeout connection failures (actual errors)
                // This indicates a real problem, not just Desktop service not running
                if (IsConnected)
                {
                    // Connection was established but lost - log the disconnection
                    _logger.LogWarning(ex, "Connection to desktop service lost");
                }
                else
                {
                    // First connection attempt failed with error (not timeout)
                    // Don't log here - will be logged when connection state changes
                }
            }
            finally
            {
                var wasConnected = IsConnected;
                IsConnected = false;
                _service = null;
                _jsonRpc?.Dispose();
                _pipeStream?.Dispose();
                _jsonRpc = null;
                _pipeStream = null;
                
                // Only log disconnection if we were actually connected
                if (wasConnected)
                {
                    _logger.LogInformation("Disconnected from desktop service");
                }
            }

            // Wait before retrying
            try
            {
                await Task.Delay(2000, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

