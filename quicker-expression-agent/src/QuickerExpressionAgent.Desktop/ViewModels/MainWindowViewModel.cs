using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Windows.Media;

namespace QuickerExpressionAgent.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IExpressionAgentToolHandler
    {
        private readonly SemanticKernelExpressionAgent _semanticKernelAgent;
        private readonly RoslynExpressionService _roslynService;
        private readonly ILogger<MainWindowViewModel> _logger;

        // Throttle for auto-execution
        private DateTime _lastAutoExecutionTime = DateTime.MinValue;
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


        public MainWindowViewModel(
            IKernelService kernelService,
            RoslynExpressionService roslynService,
            ILogger<MainWindowViewModel> logger)
        {
            _logger = logger;
            _roslynService = roslynService;
            _semanticKernelAgent = new SemanticKernelExpressionAgent(kernelService.Kernel, _roslynService, this);


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
                SemanticKernelExpressionAgent.AgentStreamingCallback? streamingCallback = (stepType, partialContent, isComplete) =>
                {
                    if (!string.IsNullOrEmpty(partialContent))
                    {
                        // Append content to assistant message on UI thread
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new System.Action(() =>
                            {
                                assistantMessage.Content += partialContent;
                            }));
                    }
                };

                // Progress callback for tool calls (optional, can be used for tool call display)
                SemanticKernelExpressionAgent.AgentProgressCallback? progressCallback = null;

                // Agent will call tools (SetExpression, TestExpression, etc.) to complete the work
                // Agent can get existing variables via GetExternalVariables tool
                // No need to process result - tools have already updated the UI
                await _semanticKernelAgent.GenerateExpressionAsync(
                    text,
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
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteExpressionInternalAsync()
        {
            if (string.IsNullOrWhiteSpace(Expression))
            {
                ExecutionResult = "没有可执行的表达式";
                ResultForeground = Brushes.Orange;
                return;
            }

            if (_roslynService == null)
            {
                ExecutionResult = "Roslyn 服务未初始化";
                ResultForeground = Brushes.Red;
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
                // Convert variable list to VariableClass list
                var variableClassList = VariableList.Select(v => v.ToVariableClass()).ToList();

                // Execute expression
                var result = await _roslynService.ExecuteExpressionAsync(Expression, variableClassList);

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
            catch (System.Exception ex)
            {
                ExecutionResult = $"✗ 发生异常: {ex.Message}";
                ResultForeground = Brushes.Red;
                StatusText = "发生错误";
                _logger?.LogError(ex, "Error executing expression");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        /// <summary>
        /// Update variable list, preserving existing variable values if they already exist
        /// </summary>
        private void UpdateVariableList(List<VariableClass> newVariables)
        {
            // Unsubscribe from old variables
            foreach (var variable in VariableList)
            {
                variable.ValueChanged -= OnVariableValueChanged;
            }

            // Create a dictionary of existing variables by name for quick lookup
            // Also store their original default values for comparison
            var existingVariables = VariableList.ToDictionary(v => v.VarName, StringComparer.OrdinalIgnoreCase);
            var existingVariableDefaults = VariableList.ToDictionary(
                v => v.VarName,
                v => v.ToVariableClass().DefaultValue,
                StringComparer.OrdinalIgnoreCase);

            // Clear the list and rebuild it
            VariableList.Clear();

            foreach (var newVariable in newVariables)
            {
                VariableItemViewModel variableViewModel;

                // Check if variable already exists
                if (existingVariables.TryGetValue(newVariable.VarName, out var existingVar))
                {
                    // Variable exists - check if type changed
                    if (existingVar.VarType != newVariable.VarType)
                    {
                        // Type changed, create new ViewModel with new type and default value
                        variableViewModel = new VariableItemViewModel(newVariable);
                    }
                    else
                    {
                        // Type unchanged, keep existing ViewModel but update default if user hasn't modified it
                        variableViewModel = existingVar;

                        // Get old default value
                        existingVariableDefaults.TryGetValue(newVariable.VarName, out var oldDefaultValue);

                        // Update default value if user hasn't modified it
                        variableViewModel.UpdateDefaultValueIfUnchanged(newVariable, oldDefaultValue);
                    }
                }
                else
                {
                    // New variable, create new ViewModel with default value
                    variableViewModel = new VariableItemViewModel(newVariable);
                }

                // Subscribe to value changes for auto-execution
                variableViewModel.ValueChanged += OnVariableValueChanged;
                VariableList.Add(variableViewModel);
            }
        }

        /// <summary>
        /// Handle variable value change - auto-execute expression with throttle
        /// </summary>
        private async void OnVariableValueChanged(object? sender, EventArgs e)
        {
            // Only auto-execute if there's a parsed expression
            if (string.IsNullOrWhiteSpace(Expression))
            {
                return;
            }

            if (_roslynService == null)
            {
                return;
            }

            // Cancel previous pending execution
            _autoExecutionCancellationTokenSource?.Cancel();
            _autoExecutionCancellationTokenSource?.Dispose();
            _autoExecutionCancellationTokenSource = new System.Threading.CancellationTokenSource();
            var cancellationToken = _autoExecutionCancellationTokenSource.Token;

            // Throttle: check if enough time has passed since last execution
            var timeSinceLastExecution = (DateTime.Now - _lastAutoExecutionTime).TotalMilliseconds;
            if (timeSinceLastExecution < 500)
            {
                // Wait for the remaining time
                var waitTime = 500 - (int)timeSinceLastExecution;
                try
                {
                    await System.Threading.Tasks.Task.Delay(waitTime, cancellationToken);
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    // Cancelled by new change, exit
                    return;
                }
            }

            // Check if still valid (user might have changed expression or cancelled)
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Expression) && _roslynService != null)
            {
                // Update last execution time
                _lastAutoExecutionTime = DateTime.Now;

                // Execute expression with updated variable values (without blocking UI)
                await ExecuteExpressionInternalAsync();
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
                // Try to serialize as JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                return JsonSerializer.Serialize(value, options);
            }
            catch
            {
                // If serialization fails, return string representation
                return value.ToString() ?? "null";
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
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
                        // Update variable type if changed
                        if (existingVariable.VarType != variable.VarType)
                        {
                            existingVariable.VarType = variable.VarType;
                            existingVariable.IsListType = variable.VarType == VariableType.ListString;
                            existingVariable.IsDictionaryType = variable.VarType == VariableType.Dictionary;
                        }

                        // Convert DefaultValue to string representation using VariableItemViewModel's conversion logic
                        // Use the same logic as VariableItemViewModel constructor
                        string valueText = ConvertValueToString(variable.DefaultValue, variable.VarType);
                        existingVariable.ValueText = valueText;
                    }
                    catch (Exception)
                    {
                        // Ignore conversion errors
                    }
                }
                else
                {
                    // Create new variable
                    // Use the provided DefaultValue directly, or get default if null
                    object? defaultVal = variable.DefaultValue;
                    if (defaultVal == null)
                    {
                        defaultVal = variable.VarType.GetDefaultValue();
                    }

                    // Create variable view model
                    var variableViewModel = new VariableItemViewModel(
                        new VariableClass
                        {
                            VarName = variable.VarName,
                            VarType = variable.VarType,
                            DefaultValue = defaultVal
                        });

                    // Subscribe to value changes for auto-execution
                    variableViewModel.ValueChanged += OnVariableValueChanged;

                    // Add to variable list
                    VariableList.Add(variableViewModel);
                }
            });
        }

        /// <summary>
        /// Convert value to string representation (same logic as VariableItemViewModel)
        /// </summary>
        private string ConvertValueToString(object? value, VariableType varType)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (varType == VariableType.ListString)
            {
                if (value is System.Collections.IEnumerable enumerable)
                {
                    var items = enumerable.Cast<object>().Select(item => item?.ToString() ?? "").ToList();
                    return string.Join("\n", items);
                }
                return string.Empty;
            }

            if (varType == VariableType.Dictionary)
            {
                if (value is System.Collections.IDictionary dict)
                {
                    // Convert Dictionary to JSON format
                    var jsonDict = new Dictionary<string, object?>();
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        jsonDict[entry.Key?.ToString() ?? ""] = entry.Value;
                    }
                    return System.Text.Json.JsonSerializer.Serialize(jsonDict, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                return "{}";
            }

            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.GetRawText();
            }

            return value.ToString() ?? string.Empty;
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
            var newVariable = new VariableItemViewModel(
                new VariableClass
                {
                    VarName = varName,
                    VarType = VariableType.String,
                    DefaultValue = string.Empty
                });
            
            // Subscribe to value changes for auto-execution
            newVariable.ValueChanged += OnVariableValueChanged;
            
            // Add to variable list
            VariableList.Add(newVariable);
        }
        
        /// <summary>
        /// Test an expression for syntax and execution
        /// </summary>
        public async Task<ExpressionResult> TestExpression(string expression, List<VariableClass>? variables = null)
        {
            if (_roslynService == null)
            {
                return new ExpressionResult
                {
                    Success = false,
                    Error = "Roslyn service not available"
                };
            }

            if (string.IsNullOrEmpty(expression))
            {
                return new ExpressionResult
                {
                    Success = false,
                    Error = "Expression cannot be empty."
                };
            }

            try
            {
                // If variables not provided, get current variables from UI
                var variablesToUse = variables ?? VariableList.Select(v => v.ToVariableClass()).ToList();
                
                var result = await _roslynService.ExecuteExpressionAsync(
                    expression,
                    variablesToUse);

                // Add chat message to show test result (UI update, keep in ViewModel)
                RunOnUIThread(() =>
                {
                    if (result.Success)
                    {
                        // Create ExpressionItemViewModel for display
                        var expressionItem = new ExpressionItemViewModel();
                        expressionItem.RoslynService = _roslynService; // Inject Roslyn service for execution
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
                        expressionItem.RoslynService = _roslynService; // Inject Roslyn service for execution
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
                return new ExpressionResult
                {
                    Success = false,
                    Error = $"Error testing expression: {ex.Message}"
                };
            }
        }

        #endregion
    }

}

