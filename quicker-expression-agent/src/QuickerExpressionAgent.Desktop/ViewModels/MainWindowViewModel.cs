using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace QuickerExpressionAgent.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IExpressionAgentToolHandler
    {
        private readonly ExpressionAgentViewModel _agentViewModel;
        private readonly ExpressionExecutor _executor;
        private readonly ILogger<MainWindowViewModel> _logger;
        
        // Chat history managed by caller
        private readonly ChatHistory _chatHistory = new();

        // Cancellation token for auto-execution
        private System.Threading.CancellationTokenSource? _autoExecutionCancellationTokenSource;

        // Chat messages and input
        [ObservableProperty]
        public partial ObservableCollection<ChatMessageViewModel> ChatMessages { get; set; } = new();

        [ObservableProperty]
        public partial string ChatInputText { get; set; } = string.Empty;

        // Subscription for scroll-to-bottom signal using DynamicData
        private IDisposable? _scrollThrottleSubscription;

        // Event to signal that chat should scroll to bottom
        public event EventHandler? ChatScrollToBottomRequested;

        [ObservableProperty]
        private string _expression = string.Empty;

        [ObservableProperty]
        private ObservableCollection<VariableItemViewModel> _variableList = new();

        [ObservableProperty]
        private string _executionResult = string.Empty;

        [ObservableProperty]
        private string _statusText = "就绪";

        [ObservableProperty]
        private bool _isGenerating = false;

        [ObservableProperty]
        private Brush _resultForeground = Brushes.Black;

        [ObservableProperty]
        private string _currentApiDisplayText = "未配置";

        /// <summary>
        /// Expose ExpressionAgentViewModel for binding
        /// </summary>
        public ExpressionAgentViewModel AgentViewModel => _agentViewModel;

        private readonly IServiceProvider _serviceProvider;

        public MainWindowViewModel(
            ExpressionAgentViewModel agentViewModel,
            ExpressionExecutor executor,
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _executor = executor;
            _serviceProvider = serviceProvider;
            _agentViewModel = agentViewModel;

            // Set this as tool handler for the agent
            _agentViewModel.SetToolHandler(this);

            // Listen for agent recreation events
            _agentViewModel.AgentRecreated += OnAgentRecreated;

            // Update status text from agent view model
            StatusText = _agentViewModel.StatusText;

            // Update display
            UpdateCurrentApiDisplay();

            StatusText = "已就绪，可以开始生成表达式";

            // Initialize chat messages monitoring for auto-scroll
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            _scrollThrottleSubscription = ChatMessages
                .ToObservableChangeSet()
                .Where(changes => changes.Any(c => c.Reason is ListChangeReason.Add or
                                                   ListChangeReason.Replace))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOnDispatcher()
                .Subscribe(_ => ChatScrollToBottomRequested?.Invoke(this, EventArgs.Empty));

            // Monitor variable value changes using DynamicData
            VariableList
                .ToObservableChangeSet()
                .MergeMany(variable => variable.WhenPropertyChanged(v => v.ValueText, notifyOnInitialValue: false))
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => ExecuteExpressionInternalAsync().ConfigureAwait(false));

            // Initialize chat with welcome message
            AddChatMessage(ChatMessageType.Assistant, "Agent 已就绪，等待您的指令...");
        }

        /// <summary>
        /// Add a chat message to the conversation
        /// </summary>
        public void AddChatMessage(ChatMessageType messageType, string content)
        {
            ChatMessages.Add(new ChatMessageViewModel(messageType, content));
        }

        [RelayCommand]
        private async Task GenerateAsync()
        {
            var text = ChatInputText.Trim();
            ChatInputText = "";
            if (string.IsNullOrWhiteSpace(text))
            {
                ExecutionResult = "请输入自然语言描述";
                ResultForeground = Brushes.Orange;
                return;
            }

            // Check if agent is initialized
            var agent = _agentViewModel.Agent;
            if (agent == null)
            {
                ExecutionResult = "✗ Agent 未初始化，请先配置 API Key";
                ResultForeground = Brushes.Red;
                StatusText = "Agent 未初始化";
                return;
            }

            // Update UI state
            IsGenerating = true;
            StatusText = "正在生成表达式...";

            // Add user message
            AddChatMessage(ChatMessageType.User, text);

            // Create assistant message for streaming content
            var assistantMessage = new ChatMessageViewModel(ChatMessageType.Assistant, string.Empty);
            ChatMessages.Add(assistantMessage);

            try
            {
                // Streaming callback - append content to assistant message (like demo project)
                ExpressionAgent.AgentStreamingCallback? streamingCallback = (stepType, partialContent, isComplete) =>
                {
                    if (!string.IsNullOrEmpty(partialContent))
                    {
                        // Append content to assistant message on UI thread
                        Application.Current.Dispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Background, () =>
                            {
                                assistantMessage.Content += partialContent;
                            });
                    }
                };

                // Progress callback for tool calls (optional, can be used for tool call display)
                ExpressionAgent.AgentProgressCallback? progressCallback = null;

                // Agent will call tools (SetExpression, TestExpression, etc.) to complete the work
                // Agent can get existing variables via GetExternalVariables tool
                // No need to process result - tools have already updated the UI
                await agent.GenerateExpressionAsync(
                    text,
                    _chatHistory,
                    progressCallback: progressCallback,
                    streamingCallback: streamingCallback,
                    cancellationToken: CancellationToken.None);

                // Agent has completed - tools have already updated expression and variables
                StatusText = "Agent 已完成";
            }
            catch (System.Exception ex)
            {
                ExecutionResult = $"✗ 发生异常: {ex.Message}";
                ResultForeground = Brushes.Red;
                StatusText = "发生错误";
                _logger?.LogError(ex, "Error generating expression");
                assistantMessage.Content += $"\n\n发生异常: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ExecuteExpressionAsync()
        {
            await ExecuteExpressionInternalAsync();
        }

        /// <summary>
        /// Internal method to execute expression (can be called from command or auto-execution)
        /// Automatically cancels previous execution if called again
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteExpressionInternalAsync()
        {
            // Cancel previous pending execution
            _autoExecutionCancellationTokenSource?.Cancel();
            _autoExecutionCancellationTokenSource?.Dispose();
            _autoExecutionCancellationTokenSource = new System.Threading.CancellationTokenSource();
            var cancellationToken = _autoExecutionCancellationTokenSource.Token;

            if (string.IsNullOrWhiteSpace(Expression))
            {
                ExecutionResult = "没有可执行的表达式";
                ResultForeground = Brushes.Orange;
                return;
            }

            // Don't set IsGenerating for auto-execution to avoid blocking UI
            // Only show status text
            StatusText = "正在执行表达式...";
            var previousResult = ExecutionResult;
            ExecutionResult = string.Empty;
            ResultForeground = Brushes.Black;

            try
            {
                // Check if cancelled before execution
                cancellationToken.ThrowIfCancellationRequested();

                // Convert variable list to VariableClass list
                var variableClassList = VariableList.Select(v => v.ToVariableClass()).ToList();

                // Execute expression
                var result = await _executor.ExecuteExpressionAsync(Expression, variableClassList);
                
                // Check if cancelled after execution
                cancellationToken.ThrowIfCancellationRequested();

                if (result.Success)
                {
                    var resultText = FormatExecutionResult(result.Value);
                    ExecutionResult = $"✓ 执行成功\n\n结果:\n{resultText}";
                    ResultForeground = Brushes.Green;
                    // Only update status if not already set (to preserve generation status)
                    if (StatusText == "正在执行表达式...")
                    {
                        StatusText = "执行成功";
                    }
                }
                else
                {
                    ExecutionResult = $"✗ 执行失败\n错误: {result.Error}";
                    ResultForeground = Brushes.Red;
                    // Only update status if not already set (to preserve generation status)
                    if (StatusText == "正在执行表达式...")
                    {
                        StatusText = "执行失败";
                    }
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                // Cancelled by new execution, ignore silently
            }
            catch (System.Exception ex)
            {
                ExecutionResult = $"✗ 发生异常: {ex.Message}";
                ResultForeground = Brushes.Red;
                StatusText = "发生错误";
                _logger?.LogError(ex, "Error executing expression");
            }
        }

        /// <summary>
        /// Run action on UI thread asynchronously (for use in tool handlers called from background threads)
        /// </summary>
        private void RunOnUIThread(Action action, System.Windows.Threading.DispatcherPriority priority = System.Windows.Threading.DispatcherPriority.Background)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(priority, action);
        }



        /// <summary>
        /// Format execution result, try to serialize as JSON if possible
        /// </summary>
        private string FormatExecutionResult(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            try
            {
                return value.ToJson(indented: true);
            }
            catch
            {
                // If serialization fails, return string representation
                return value.ToString() ?? "null";
            }
        }

        /// <summary>
        /// Handle agent recreation events
        /// </summary>
        private void OnAgentRecreated(object? sender, ExpressionAgent? agent)
        {
            // Update status text from agent view model
            StatusText = _agentViewModel.StatusText;
            UpdateCurrentApiDisplay();
        }

        /// <summary>
        /// Update current API display
        /// </summary>
        private void UpdateCurrentApiDisplay()
        {
            var current = _agentViewModel.CurrentConfig;
            if (current != null && !string.IsNullOrWhiteSpace(current.ModelId))
            {
                CurrentApiDisplayText = $"{current.ModelId} ({current.BaseUrl})";
            }
            else
            {
                CurrentApiDisplayText = "未配置";
            }
        }

        /// <summary>
        /// Switch to a different API configuration
        /// </summary>
        [RelayCommand]
        private void SwitchModel(ModelApiConfig? config)
        {
            if (config == null)
            {
                return;
            }

            if (IsGenerating)
            {
                StatusText = "正在生成中，无法切换模型";
                return;
            }

            try
            {
                _agentViewModel.SwitchConfig(config);
                // Agent will be recreated by ExpressionAgentViewModel
            }
            catch (Exception ex)
            {
                StatusText = $"切换模型失败: {ex.Message}";
                _logger?.LogError(ex, "Error switching model");
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            if (_agentViewModel != null)
            {
                _agentViewModel.AgentRecreated -= OnAgentRecreated;
            }
            _scrollThrottleSubscription?.Dispose();
            _autoExecutionCancellationTokenSource?.Dispose();
        }

        #region IExpressionAgentToolHandler Implementation

        // Expression property is already defined as [ObservableProperty] above

        /// <summary>
        /// Set or update a variable
        /// </summary>
        public void SetVariable(VariableClass variable)
        {
            RunOnUIThread(() =>
            {
                var existingVariable = VariableList.FirstOrDefault(v =>
                    string.Equals(v.VarName, variable.VarName, StringComparison.OrdinalIgnoreCase));

                if (existingVariable != null)
                {
                    // Update existing variable
                    try
                    {
                        existingVariable.VarType = variable.VarType;
                        // Direct string assignment, no conversion needed
                        existingVariable.SetDefaultValue(variable.DefaultValue);
                    }
                    catch (Exception)
                    {
                        // Ignore conversion errors
                    }
                }
                else
                {
                    // Create new variable using VariableClass constructor (directly uses string DefaultValue)
                    var variableViewModel = new VariableItemViewModel(variable);

                    // Add to variable list (DynamicData automatically monitors ValueText changes)
                    VariableList.Add(variableViewModel);
                }
            });
        }


        /// <summary>
        /// Get a specific variable by name
        /// </summary>
        public VariableClass? GetVariable(string name)
        {
            VariableClass? result = null;

            RunOnUIThread(() =>
            {
                var variableViewModel = VariableList.FirstOrDefault(v =>
                    string.Equals(v.VarName, name, StringComparison.OrdinalIgnoreCase));

                if (variableViewModel != null)
                {
                    result = variableViewModel.ToVariableClass();
                }
            });

            return result;
        }

        /// <summary>
        /// Get all variables
        /// </summary>
        public List<VariableClass> GetAllVariables()
        {
            List<VariableClass> result = new();

            RunOnUIThread(() =>
            {
                result = VariableList.Select(v => v.ToVariableClass()).ToList();
            });

            return result;
        }

        /// <summary>
        /// Create a new variable with default name and type
        /// </summary>
        [RelayCommand]
        private void CreateVariable()
        {
            // Generate a unique variable name
            var baseName = "var";
            var counter = 1;
            var varName = $"{baseName}{counter}";

            // Find a unique name
            while (VariableList.Any(v => string.Equals(v.VarName, varName, StringComparison.OrdinalIgnoreCase)))
            {
                counter++;
                varName = $"{baseName}{counter}";
            }

            // Create new variable with default type (String)
            var newVariable = new VariableItemViewModel(varName, VariableType.String, string.Empty);

            // Add to variable list (DynamicData automatically monitors ValueText changes)
            VariableList.Add(newVariable);
        }

        /// <summary>
        /// Test an expression for syntax and execution
        /// </summary>
        public async Task<ExpressionResult> TestExpressionAsync(string expression, List<VariableClass>? variables = null)
        {
            if (_executor == null)
            {
                return new ExpressionResultError("Roslyn service not available");
            }

            if (string.IsNullOrEmpty(expression))
            {
                return new ExpressionResultError("Expression cannot be empty.");
            }

            try
            {
                // If variables not provided, get current variables from UI
                var variablesToUse = variables ?? VariableList.Select(v => v.ToVariableClass()).ToList();

                var result = await _executor.ExecuteExpressionAsync(
                    expression,
                    variablesToUse);

                // Add chat message to show test result (UI update, keep in ViewModel)
                RunOnUIThread(() =>
                {
                    if (result.Success)
                    {
                        // Create ExpressionItemViewModel for display
                        var expressionItem = new ExpressionItemViewModel();
                        expressionItem.Executor = _executor; // Inject executor for execution
                        expressionItem.Initialize(result.UsedVariables, expression);
                        expressionItem.SetExecutionResult(true, result.Value);

                        // Add ExpressionItem to chat message
                        var chatMessage = new ChatMessageViewModel(ChatMessageType.Assistant, expressionItem);
                        ChatMessages.Add(chatMessage);
                    }
                    else
                    {
                        // Create ExpressionItemViewModel for display even on failure
                        var expressionItem = new ExpressionItemViewModel();
                        expressionItem.Executor = _executor; // Inject executor for execution
                        expressionItem.Initialize(result.UsedVariables, expression);
                        expressionItem.SetExecutionResult(false, null, result.Error);

                        // Add ExpressionItem to chat message
                        var chatMessage = new ChatMessageViewModel(ChatMessageType.Assistant, expressionItem);
                        ChatMessages.Add(chatMessage);
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                return new ExpressionResultError($"Error testing expression: {ex.Message}");
            }
        }

        #endregion

        [RelayCommand]
        private void OpenTestWindow()
        {
            // Check if window is already open
            var existingWindow = System.Windows.Application.Current.Windows.OfType<QuickerServiceTestWindow>().FirstOrDefault();
            if (existingWindow != null)
            {
                // Window already exists, activate it
                existingWindow.Activate();
                if (existingWindow.WindowState == System.Windows.WindowState.Minimized)
                {
                    existingWindow.WindowState = System.Windows.WindowState.Normal;
                }
            }
            else
            {
                // Create new window
                var testWindow = _serviceProvider.GetRequiredService<QuickerServiceTestWindow>();
                testWindow.Show();
            }
        }

        [RelayCommand]
        private void OpenChatWindow()
        {
            // Check if window is already open
            var existingWindow = System.Windows.Application.Current.Windows.OfType<ChatWindow>().FirstOrDefault();
            if (existingWindow != null)
            {
                // Window already exists, activate it
                existingWindow.Activate();
                if (existingWindow.WindowState == System.Windows.WindowState.Minimized)
                {
                    existingWindow.WindowState = System.Windows.WindowState.Normal;
                }
            }
            else
            {
                // Create new window
                var chatWindow = _serviceProvider.GetRequiredService<ChatWindow>();
                chatWindow.Show();
            }
        }

    }

}

