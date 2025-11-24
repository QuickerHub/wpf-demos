using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Media;
using static QuickerExpressionAgent.Server.Agent.ExpressionAgent;

namespace QuickerExpressionAgent.Desktop.ViewModels
{
    public partial class ExpressionGeneratorPageViewModel : ChatBoxViewModel, IExpressionAgentToolHandler
    {
        private readonly ExpressionExecutor _executor;

        // Cancellation token for auto-execution
        private System.Threading.CancellationTokenSource? _autoExecutionCancellationTokenSource;

        [ObservableProperty]
        private string _expression = string.Empty;

        [ObservableProperty]
        private ObservableCollection<VariableItemViewModel> _variableList = new();

        [ObservableProperty]
        private string _executionResult = string.Empty;

        [ObservableProperty]
        private Brush _resultForeground = Brushes.Black;

        [ObservableProperty]
        private string _currentApiDisplayText = "未配置";

        private readonly IServiceProvider _serviceProvider;

        public ExpressionGeneratorPageViewModel(
            ExpressionAgentViewModel agentViewModel,
            ExpressionExecutor executor,
            ILogger<ExpressionGeneratorPageViewModel> logger,
            IServiceProvider serviceProvider)
            : base(agentViewModel, logger)
        {
            _executor = executor;
            _serviceProvider = serviceProvider;

            // Set this as tool handler for the agent
            _agentViewModel.SetToolHandler(this);

            // Update display
            UpdateCurrentApiDisplay();

            StatusText = "已就绪，可以开始生成表达式";

            // Monitor variable value changes using DynamicData
            VariableList
                .ToObservableChangeSet()
                .MergeMany(variable => variable.WhenPropertyChanged(v => v.ValueText, notifyOnInitialValue: false))
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => ExecuteExpressionInternalAsync().ConfigureAwait(false));

            // Initialize chat with welcome message
            AddChatMessage(ChatMessageType.Assistant, "Agent 已就绪，等待您的指令...");
        }

        protected override async Task GenerateInternalAsync(string text)
        {
            // Call base implementation to handle generation
            await base.GenerateInternalAsync(text);
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
        protected override void OnAgentRecreated(object? sender, ExpressionAgent? agent)
        {
            base.OnAgentRecreated(sender, agent);
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
        public override void Dispose()
        {
            base.Dispose();
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

