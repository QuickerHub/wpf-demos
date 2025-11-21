using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Connector for the .Server project to connect to the Quicker service via named pipe using StreamJsonRpc
/// This is used by the .Server project to call methods implemented in the .Quicker project
/// Automatically connects and reconnects in background via IHostedService
/// Uses StreamJsonRpc for JSON-RPC 2.0 protocol communication
/// </summary>
public class QuickerServerClientConnector : IHostedService
{
    private NamedPipeClientStream? _pipeStream;
    private JsonRpc? _jsonRpc;
    private IQuickerService? _service;
    private readonly ILogger<QuickerServerClientConnector> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isConnected;

    public event EventHandler<bool>? ConnectionStatusChanged;

    public QuickerServerClientConnector(ILogger<QuickerServerClientConnector> logger)
    {
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _isConnected = false;
    }

    /// <summary>
    /// Get the Quicker service client
    /// </summary>
    public IQuickerService ServiceClient
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
    /// Check if connected to the Quicker service
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

    /// <summary>
    /// Wait for connection to be established
    /// </summary>
    /// <param name="timeout">Maximum time to wait for connection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connected, false if timeout</returns>
    public async Task<bool> WaitConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (_isConnected && _service != null)
        {
            return true;
        }

        var startTime = DateTime.UtcNow;
        
        while (!_isConnected && DateTime.UtcNow - startTime < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            
            await Task.Delay(100, cancellationToken);
        }

        return _isConnected && _service != null;
    }

    /// <summary>
    /// Start the hosted service and connect in background
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(EnsureConnectedAsync, cancellationToken);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop the hosted service
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        
        _jsonRpc?.Dispose();
        _pipeStream?.Dispose();
        _service = null;
        
        _logger.LogInformation("Quicker server client connector stopped");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensure connection is established, automatically reconnect if needed
    /// </summary>
    private async Task EnsureConnectedAsync()
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
                    Constants.ServerName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                using var cts = new CancellationTokenSource(5000);
                await _pipeStream.ConnectAsync(cts.Token);

                // Create JsonRpc - it will automatically respect [JsonIgnore] attributes
                // StreamJsonRpc uses System.Text.Json which respects [System.Text.Json.Serialization.JsonIgnore]
                _jsonRpc = new JsonRpc(_pipeStream, _pipeStream);
                _service = _jsonRpc.Attach<IQuickerService>();
                _jsonRpc.StartListening();

                _logger.LogInformation("Connected to expression agent service");
                IsConnected = true;

                // Wait for disconnection
                await _jsonRpc.Completion;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Connection timeout to expression agent service, will retry");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to expression agent service, will retry");
            }
            finally
            {
                IsConnected = false;
                _service = null;
                _jsonRpc?.Dispose();
                _pipeStream?.Dispose();
                _jsonRpc = null;
                _pipeStream = null;
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
