using System;
using System.Threading;
using System.Threading.Tasks;

namespace BatchRenameTool.Utils
{
    /// <summary>
    /// Helper class for throttling function calls
    /// </summary>
    public class ThrottleHelper
    {
        private readonly int _delayMs;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lock = new object();

        /// <summary>
        /// Initialize throttle helper with delay in milliseconds
        /// </summary>
        public ThrottleHelper(int delayMs)
        {
            _delayMs = delayMs;
        }

        /// <summary>
        /// Execute action with throttle - cancels previous pending execution and schedules new one
        /// </summary>
        public void Throttle(Action action)
        {
            lock (_lock)
            {
                // Cancel previous pending execution
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                // Create new cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // Schedule new execution
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_delayMs, token);
                        if (!token.IsCancellationRequested)
                        {
                            action();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelled, ignore
                    }
                });
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}

