using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Server that hosts the IQuickerService implementation via named pipe using StreamJsonRpc
/// This server runs in the .Quicker project and provides services to agent.exe
/// Uses StreamJsonRpc for JSON-RPC 2.0 protocol communication
/// </summary>
public class QuickerServiceServer : IHostedService
{
    private readonly QuickerServiceImplementation _serviceImplementation;
    private readonly ILogger<QuickerServiceServer> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly string _serverName;
    private NamedPipeServerStream? _pipeStream;
    private JsonRpc? _jsonRpc;
    private bool _isClientConnected;

    public event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Gets whether a client is currently connected
    /// </summary>
    public bool IsClientConnected
    {
        get => _isClientConnected;
        private set
        {
            if (_isClientConnected != value)
            {
                _isClientConnected = value;
                ConnectionStatusChanged?.Invoke(this, value);
            }
        }
    }

    public QuickerServiceServer(
        QuickerServiceImplementation serviceImplementation,
        ILogger<QuickerServiceServer> logger)
    {
        _serviceImplementation = serviceImplementation;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _serverName = Constants.ServerName;
        _isClientConnected = false;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start server in background to avoid blocking IHostedService.StartAsync
        _ = Task.Run(async () =>
        {
            try
            {
                await StartServerAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start expression service server");
            }
        }, _cancellationTokenSource.Token);
        
        await Task.CompletedTask;
    }

    private async Task StartServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create named pipe server
                _pipeStream = new NamedPipeServerStream(
                    _serverName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.LogInformation("Expression service server ready, waiting for client connection on pipe: {PipeName}", _serverName);
                
                // Wait for client connection (no timeout, wait indefinitely)
                await _pipeStream.WaitForConnectionAsync(cancellationToken);
                
                _logger.LogInformation("Client connected to expression service server");
                IsClientConnected = true;

                // Create JsonRpc and attach service implementation
                _jsonRpc = new JsonRpc(_pipeStream, _pipeStream);
                _jsonRpc.AddLocalRpcTarget(_serviceImplementation);
                _jsonRpc.StartListening();

                // Wait for disconnection
                // Use ConfigureAwait(false) to avoid deadlock in async context
                await _jsonRpc.Completion.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expression service server");
            }
            finally
            {
                IsClientConnected = false;
                _jsonRpc?.Dispose();
                _pipeStream?.Dispose();
                _jsonRpc = null;
                _pipeStream = null;
            }

            // Wait a bit before accepting next connection
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        
        _jsonRpc?.Dispose();
        _pipeStream?.Dispose();
        
        _logger.LogInformation("Expression service server stopped");
        await Task.CompletedTask;
    }
}
