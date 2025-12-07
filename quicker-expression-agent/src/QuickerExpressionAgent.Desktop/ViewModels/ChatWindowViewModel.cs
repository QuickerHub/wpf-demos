using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop.Pages;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using WindowAttach.Utils;
using System.ComponentModel;
using static QuickerExpressionAgent.Server.Agent.ExpressionAgent;

namespace QuickerExpressionAgent.Desktop.ViewModels
{
    public partial class ChatWindowViewModel : ChatBoxViewModel
    {
        private readonly QuickerServerClientConnector _connector;
        private readonly ServerToolHandler _defaultToolHandler;
        private QuickerCodeEditorToolHandler? _quickerToolHandler;
        private readonly MainWindowService _mainWindowService;
        private readonly ChatWindowService _chatWindowService;

        [ObservableProperty]
        private bool _isConnected = false;

        /// <summary>
        /// Whether the chat window is connected to a code editor window
        /// </summary>
        [ObservableProperty]
        private bool _isCodeEditorConnected = false;

        [ObservableProperty]
        private string _tokenUsageText = "Token: 0";

        // Accumulated token usage counters
        private int _accumulatedInputTokens = 0;
        private int _accumulatedOutputTokens = 0;
        private int _accumulatedTotalTokens = 0;

        /// <summary>
        /// CodeEditor handler ID (for window attachment)
        /// </summary>
        public string? CodeEditorHandlerId { get; private set; }

        /// <summary>
        /// Event raised when CodeEditor handler ID changes
        /// </summary>
        public event EventHandler<string>? CodeEditorHandlerIdChanged;

        public ChatWindowViewModel(
            ExpressionAgentViewModel agentViewModel,
            ExpressionExecutor executor,
            QuickerServerClientConnector connector,
            MainWindowService mainWindowService,
            ChatWindowService chatWindowService,
            ILogger<ChatWindowViewModel> logger)
            : base(agentViewModel, logger)
        {
            _connector = connector;
            _mainWindowService = mainWindowService;
            _chatWindowService = chatWindowService ?? throw new ArgumentNullException(nameof(chatWindowService));
            
            // Create default tool handler using script engine (ServerToolHandler)
            // This is independent of Quicker connection and uses ExpressionExecutor
            _defaultToolHandler = new ServerToolHandler(executor);
            
            // Set default tool handler to agent view model
            _agentViewModel.SetToolHandler(_defaultToolHandler);
            
            // Override initial status text
            StatusText = "正在连接 Quicker 服务...";

            // Monitor connection status
            _connector.ConnectionStatusChanged += Connector_ConnectionStatusChanged;

            // Initialize connection
            InitializeConnectionAsync().ConfigureAwait(false);
            
            // Initialize token usage display
            UpdateTokenUsage();

            // Initialize chat with welcome message
            AddChatMessage(ChatMessageType.Assistant, "正在连接 Quicker 服务，请稍候...");
        }

        private void Connector_ConnectionStatusChanged(object? sender, bool isConnected)
        {
            // Ensure UI updates happen on UI thread (connector may call from background thread)
            RunOnUIThread(() =>
            {
                // Only update IsConnected flag, don't set it to true here
                // IsConnected will be set to true only after tool handler is created
                if (!isConnected)
                {
                    IsConnected = false;
                    // Switch back to default tool handler on disconnect
                if (_agentViewModel.Agent != null && _quickerToolHandler != null)
                    {
                    _agentViewModel.SetToolHandler(_defaultToolHandler);
                        _quickerToolHandler = null;
                    }
                    // Clear CodeEditor handler ID on disconnect
                    CodeEditorHandlerId = null;
                    IsCodeEditorConnected = false; // Update code editor connection status
                    StatusText = "未连接到 Quicker 服务";
                    AddChatMessage(ChatMessageType.Assistant, "✗ 与 Quicker 服务断开连接");
                }
                // When connected, don't update IsConnected here - wait for tool handler creation
            });
        }

        private async Task InitializeConnectionAsync()
        {
            try
            {
                // Wait for connection (similar to .Server project)
                var connected = await _connector.WaitConnectAsync(TimeSpan.FromSeconds(10));
                if (!connected)
                {
                    RunOnUIThread(() =>
                    {
                        StatusText = "无法连接到 Quicker 服务";
                        AddChatMessage(ChatMessageType.Assistant, "✗ 无法连接到 Quicker 服务，请确保 Quicker 应用程序正在运行");
                    });
                    return;
                }

                // Skip auto-creation if CodeEditorHandlerId is already set (e.g., set via OpenChatWindowAsync)
                if (!string.IsNullOrEmpty(CodeEditorHandlerId))
                {
                    RunOnUIThread(() =>
                    {
                        IsConnected = true;
                        StatusText = "已就绪，可以开始生成表达式";
                    });
                    return;
                }

                // Use ChatWindowService to get an available CodeEditorWindow (one without ChatWindow)
                try
                {
                    RunOnUIThread(() =>
                    {
                        StatusText = "正在查找可用的 Code Editor...";
                    });

                    // Get available CodeEditorWindow handle (one without ChatWindow)
                    var availableWindowHandle = await _chatWindowService.GetAvailableCodeEditorWindowHandleAsync();
                    
                    string? handlerId = null;

                    if (availableWindowHandle == 0)
                    {
                        // No available CodeEditorWindow (all existing ones have ChatWindows)
                        // Create a new CodeEditorWindow
                        RunOnUIThread(() =>
                        {
                            StatusText = "正在创建新的 Code Editor...";
                        });

                        handlerId = await _connector.ServiceClient.GetOrCreateCodeEditorAsync();
                        
                        if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                        {
                            RunOnUIThread(() =>
                            {
                                StatusText = "无法创建 Code Editor";
                                AddChatMessage(ChatMessageType.Assistant, "✗ 无法创建 Code Editor 窗口");
                            });
                            return;
                        }

                        // Note: ChatWindow will automatically register itself when it gets the window handle
                        // via CodeEditorHandlerIdChanged event -> TryAttachToCodeEditorAsync -> RegisterChatWindowForCodeEditor
                    }
                    else
                    {
                        // Found an available CodeEditorWindow without ChatWindow
                        // Get handler ID from window handle
                        handlerId = await _connector.ServiceClient.GetCodeWrapperIdAsync(availableWindowHandle.ToString());
                        if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                        {
                            RunOnUIThread(() =>
                            {
                                StatusText = "无法获取 Code Editor Handler ID";
                                AddChatMessage(ChatMessageType.Assistant, "✗ 无法获取 Code Editor Handler ID");
                            });
                            return;
                        }

                        // Note: ChatWindow will automatically register itself when it gets the window handle
                        // via CodeEditorHandlerIdChanged event -> TryAttachToCodeEditorAsync -> RegisterChatWindowForCodeEditor
                    }

                    if (handlerId != null)
                    {
                        // Setup connection with the CodeEditorWindow
                        await SetupCodeEditorConnectionAsync(handlerId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting or creating Code Editor");
                    RunOnUIThread(() =>
                    {
                        StatusText = "无法创建 Code Editor";
                        AddChatMessage(ChatMessageType.Assistant, $"✗ 无法创建 Code Editor 窗口: {ex.Message}");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing connection");
                RunOnUIThread(() =>
                {
                    StatusText = "初始化失败";
                    AddChatMessage(ChatMessageType.Assistant, $"✗ 初始化失败: {ex.Message}");
                });
            }
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task GenerateFromTextBoxAsync()
        {
            var text = ChatInputText.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return; // TextBox: don't handle empty text, allow default newline behavior
            }

            // If already generating, cancel the current operation (stop + send combination)
            if (IsGenerating)
            {
                _cancellationTokenSource?.Cancel();
                StatusText = "正在停止...";
                // Wait a bit for cancellation to complete
                await Task.Delay(100);
            }

            ChatInputText = "";
            await GenerateInternalAsync(text);
        }

        /// <summary>
        /// Stop generation (called when code editor window is closed or chat window is closing)
        /// </summary>
        /// <param name="reason">Reason for stopping (optional, for custom messages)</param>
        public void StopGeneration(string? reason = null)
        {
            if (IsGenerating)
            {
                _cancellationTokenSource?.Cancel();
                RunOnUIThread(() =>
                {
                    if (string.IsNullOrEmpty(reason))
                    {
                        StatusText = "Code Editor 窗口已关闭，生成已停止";
                        AddChatMessage(ChatMessageType.Assistant, "✗ Code Editor 窗口已关闭，生成已停止");
                    }
                    else
                    {
                        StatusText = reason;
                        AddChatMessage(ChatMessageType.Assistant, $"✗ {reason}");
                    }
                });
            }
        }

        /// <summary>
        /// Open API configuration page in MainWindow
        /// </summary>
        [RelayCommand]
        private void OpenApiConfig()
        {
            try
            {
                if (!_mainWindowService.ShowAndNavigate<ApiConfigPage>())
                {
                    RunOnUIThread(() =>
                    {
                        AddChatMessage(ChatMessageType.Assistant, "✗ 无法打开 API 配置页面");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open API config page");
                RunOnUIThread(() =>
                {
                    AddChatMessage(ChatMessageType.Assistant, $"✗ 无法打开 API 配置页面: {ex.Message}");
                });
            }
        }

        protected override bool CanGenerate()
        {
            // Check if Quicker tool handler is available (prefer Quicker over standalone)
            if (_quickerToolHandler == null)
            {
                // If Quicker is still connected, try to get or create code editor window
                if (IsConnected && _connector?.ServiceClient != null)
                {
                    // Note: CanGenerate is synchronous, so we can't await here
                    // The actual reconnection will happen in GenerateInternalAsync if needed
                    return false;
                }
                else
                {
                    // Ensure state is consistent: if no Quicker handler, code editor is not connected
                    if (IsCodeEditorConnected)
                    {
                        IsCodeEditorConnected = false;
                        CodeEditorHandlerId = null;
                    }
                    AddChatMessage(ChatMessageType.Assistant, "✗ Code Editor 未就绪，无法生成表达式");
                    return false;
                }
            }

            // Verify code editor window is still valid (synchronous check only)
            // Full verification will happen in GenerateInternalAsync
            return true;
        }

        protected override async Task GenerateInternalAsync(string text)
        {
            // Verify code editor window is still valid before generation
            if (!string.IsNullOrEmpty(CodeEditorHandlerId))
            {
                if (!await VerifyCodeEditorWindowAsync(CodeEditorHandlerId))
                {
                    // Code editor window has been closed, try to reconnect if Quicker is still connected
                    if (IsConnected && _connector?.ServiceClient != null)
                    {
                        if (!await TryReconnectCodeEditorAsync())
                        {
                            // Failed to reconnect
                            return;
                        }
                        // Successfully reconnected, continue with generation
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // Call base implementation to handle actual generation
            await base.GenerateInternalAsync(text);
        }

        /// <summary>
        /// Update token usage display based on current chat history
        /// If accumulated token counts are available, use them; otherwise use estimated counts
        /// </summary>
        protected override void UpdateTokenUsage()
        {
            try
            {
                var agent = _agentViewModel.Agent;
                if (agent == null)
                {
                    TokenUsageText = "Token: N/A (Agent 未初始化)";
                    return;
                }

                int messageCount = agent.GetChatHistoryCount(_chatHistory);
                
                // If we have accumulated token counts from API calls, use them
                if (_accumulatedTotalTokens > 0)
                {
                    TokenUsageText = $"Token: {_accumulatedTotalTokens:N0} (In: {_accumulatedInputTokens:N0}, Out: {_accumulatedOutputTokens:N0}) | Messages: {messageCount}";
                }
                else
                {
                    // Otherwise, use estimated token count from chat history
                    int tokenCount = agent.EstimateTokenCount(_chatHistory);
                    TokenUsageText = $"Token: {tokenCount:N0} | Messages: {messageCount}";
                }
            }
            catch
            {
                // Ignore errors in token calculation
                TokenUsageText = "Token: N/A";
            }
        }

        /// <summary>
        /// Update token usage display from TokenUsageStreamItem (actual API usage)
        /// Accumulates token counts across multiple API calls
        /// </summary>
        protected override void UpdateTokenUsageFromStreamItem(TokenUsageStreamItem tokenUsageItem)
        {
            try
            {
                // Accumulate token counts
                _accumulatedInputTokens += tokenUsageItem.InputTokenCount;
                _accumulatedOutputTokens += tokenUsageItem.OutputTokenCount;
                _accumulatedTotalTokens += tokenUsageItem.TotalTokenCount;

                int messageCount = _agentViewModel.Agent?.GetChatHistoryCount(_chatHistory) ?? 0;
                TokenUsageText = $"Token: {_accumulatedTotalTokens:N0} (In: {_accumulatedInputTokens:N0}, Out: {_accumulatedOutputTokens:N0}) | Messages: {messageCount}";
            }
            catch
            {
                // Ignore errors in token calculation
                TokenUsageText = "Token: N/A";
            }
        }


        /// <summary>
        /// Handle agent recreation events
        /// </summary>
        protected override void OnAgentRecreated(object? sender, ExpressionAgent? agent)
        {
            // Update tool handler if Quicker is connected
            if (_quickerToolHandler != null && agent != null)
            {
                _agentViewModel.SetToolHandler(_quickerToolHandler);
            }
            else if (_quickerToolHandler == null && agent != null)
            {
                // If Quicker handler is not available, use default handler
                _agentViewModel.SetToolHandler(_defaultToolHandler);
                // Ensure state is consistent
                if (IsCodeEditorConnected)
                {
                    IsCodeEditorConnected = false;
                    CodeEditorHandlerId = null;
                }
            }
            
            // Update status text from agent view model
            StatusText = _agentViewModel.StatusText;
            UpdateTokenUsage();
        }

        /// <summary>
        /// Get CodeEditor window handle by handler ID
        /// </summary>
        public async Task<long> GetCodeEditorWindowHandleAsync(string handlerId)
        {
            try
            {
                if (_connector?.ServiceClient == null)
                    return 0;

                return await _connector.ServiceClient.GetWindowHandleAsync(handlerId);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Set CodeEditor handler ID from window handle (to prevent auto-creation)
        /// This method tries to get handler ID from the window handle and sets it to prevent automatic CodeEditor creation
        /// </summary>
        /// <param name="windowHandle">Window handle to get handler ID from</param>
        /// <returns>True if handler ID was found and set, false otherwise</returns>
        public async Task<bool> SetCodeEditorHandlerIdFromWindowHandleAsync(long windowHandle)
        {
            try
            {
                // Wait for connection if not connected yet
                if (!IsConnected && _connector != null)
                {
                    var connected = await _connector.WaitConnectAsync(TimeSpan.FromSeconds(5));
                    if (!connected)
                    {
                        return false;
                    }
                }

                if (_connector?.ServiceClient == null)
                    return false;

                // Try to get handler ID from window handle
                var handlerId = await _connector.ServiceClient.GetCodeWrapperIdAsync(windowHandle.ToString());
                
                if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                {
                    return false;
                }

                // Set handler ID to prevent auto-creation
                RunOnUIThread(() =>
                {
                    CodeEditorHandlerId = handlerId;
                    CodeEditorHandlerIdChanged?.Invoke(this, handlerId);
                    
                    // Create handler using handlerId
                    _quickerToolHandler = new QuickerCodeEditorToolHandler(handlerId, _connector);
                    _agentViewModel.SetToolHandler(_quickerToolHandler);
                    
                    IsConnected = true;
                    IsCodeEditorConnected = true;
                    StatusText = "已连接到 Code Editor";
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Setup connection with CodeEditorWindow using handler ID
        /// </summary>
        private async Task SetupCodeEditorConnectionAsync(string handlerId)
        {
            try
            {
                // Create handler using handlerId
                _quickerToolHandler = new QuickerCodeEditorToolHandler(handlerId, _connector);
                _agentViewModel.SetToolHandler(_quickerToolHandler);

                RunOnUIThread(() =>
                {
                    IsConnected = true; // Update connection status
                    IsCodeEditorConnected = true; // Update code editor connection status
                    StatusText = "已就绪，可以开始生成表达式";
                    AddChatMessage(ChatMessageType.Assistant, $"✓ Code Editor 已打开 (Handler ID: {handlerId})，可以开始生成表达式");
                    
                    // Update CodeEditor handler ID and raise event
                    CodeEditorHandlerId = handlerId;
                    CodeEditorHandlerIdChanged?.Invoke(this, handlerId);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up Code Editor connection");
                RunOnUIThread(() =>
                {
                    StatusText = "无法设置 Code Editor 连接";
                    AddChatMessage(ChatMessageType.Assistant, $"✗ 无法设置 Code Editor 连接: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Try to reconnect to code editor window (get existing or create new)
        /// </summary>
        private async Task<bool> TryReconnectCodeEditorAsync()
        {
            try
            {
                RunOnUIThread(() =>
                {
                    StatusText = "正在重新连接 Code Editor...";
                });

                // Use ChatWindowService to get an available CodeEditorWindow
                var availableWindowHandle = await _chatWindowService.GetAvailableCodeEditorWindowHandleAsync();
                
                if (availableWindowHandle == 0)
                {
                    RunOnUIThread(() =>
                    {
                        StatusText = "无法获取 Code Editor";
                        AddChatMessage(ChatMessageType.Assistant, "✗ 无法获取 Code Editor 窗口");
                    });
                    return false;
                }

                // Get handler ID from window handle
                var handlerId = await _connector.ServiceClient.GetCodeWrapperIdAsync(availableWindowHandle.ToString());
                
                if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                {
                    RunOnUIThread(() =>
                    {
                        StatusText = "无法获取 Code Editor Handler ID";
                        AddChatMessage(ChatMessageType.Assistant, "✗ 无法获取 Code Editor Handler ID");
                    });
                    return false;
                }

                // Setup connection
                await SetupCodeEditorConnectionAsync(handlerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconnecting to Code Editor");
                RunOnUIThread(() =>
                {
                    StatusText = "无法重新连接 Code Editor";
                    AddChatMessage(ChatMessageType.Assistant, "✗ 无法重新连接 Code Editor 窗口");
                });
                return false;
            }
        }

        /// <summary>
        /// Verify if code editor window is still valid
        /// </summary>
        private async Task<bool> VerifyCodeEditorWindowAsync(string handlerId)
        {
            try
            {
                var windowHandle = await GetCodeEditorWindowHandleAsync(handlerId);
                if (windowHandle == 0)
                {
                    // Window handle is zero, window is closed
                    HandleCodeEditorWindowClosed();
                    return false;
                }

                // Verify window actually exists using WindowHelper
                var codeEditorHandle = new IntPtr(windowHandle);
                if (!WindowHelper.IsWindow(codeEditorHandle))
                {
                    // Window handle is invalid, window is closed
                    HandleCodeEditorWindowClosed();
                    return false;
                }

                return true;
            }
            catch
            {
                // If we can't verify, assume it's still valid and continue
                return true;
            }
        }

        /// <summary>
        /// Handle code editor window closed event
        /// </summary>
        private void HandleCodeEditorWindowClosed()
        {
            RunOnUIThread(() =>
            {
                IsCodeEditorConnected = false;
                CodeEditorHandlerId = null;
                _quickerToolHandler = null;
                _agentViewModel.SetToolHandler(_defaultToolHandler);
                StatusText = "Code Editor 窗口已关闭";
                AddChatMessage(ChatMessageType.Assistant, "✗ Code Editor 窗口已关闭");
            });
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _connector.ConnectionStatusChanged -= Connector_ConnectionStatusChanged;
        }
    }
}


