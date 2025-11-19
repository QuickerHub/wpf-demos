using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for displaying parsed expression item (VarDefine + Expression)
/// </summary>
public partial class ExpressionItemViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<VariableItemViewModel> _variableList = new();

    [ObservableProperty]
    private string _expression = string.Empty;

    [ObservableProperty]
    private string _language = "csharp"; // For syntax highlighting (default C#)

    [ObservableProperty]
    private bool _isExpanded = true; // Collapse/expand state

    [ObservableProperty]
    private bool _showVariables = true; // Show full variable list or collapsed view

    /// <summary>
    /// Collapsed variable display text (simplified view)
    /// Format: "变量: x (Int), y (String), ..."
    /// </summary>
    [ObservableProperty]
    private string _collapsedVariableText = string.Empty;

    /// <summary>
    /// Execution result text (JSON formatted)
    /// </summary>
    [ObservableProperty]
    private string _executionResult = string.Empty;

    /// <summary>
    /// Whether the expression execution was successful
    /// </summary>
    [ObservableProperty]
    private bool _executionSuccess = false;

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    [ObservableProperty]
    private string _executionError = string.Empty;

    /// <summary>
    /// Whether to show execution result section
    /// </summary>
    [ObservableProperty]
    private bool _showExecutionResult = false;

    /// <summary>
    /// Whether expression is currently executing
    /// </summary>
    [ObservableProperty]
    private bool _isExecuting = false;

    /// <summary>
    /// Roslyn service for executing expressions (injected from outside)
    /// </summary>
    public IRoslynExpressionService? RoslynService { get; set; }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExecuteAsync()
    {
        if (RoslynService == null)
        {
            SetExecutionResult(false, null, "Roslyn 服务未初始化");
            return;
        }

        if (string.IsNullOrWhiteSpace(Expression))
        {
            SetExecutionResult(false, null, "表达式为空");
            return;
        }

        IsExecuting = true;
        try
        {
            // Convert VariableList to VariableClass list
            var variableClassList = VariableList.Select(v => v.ToVariableClass()).ToList();

            // Execute expression
            var result = await RoslynService.ExecuteExpressionAsync(Expression, variableClassList);

            if (result.Success)
            {
                SetExecutionResult(true, result.Value, null);
            }
            else
            {
                SetExecutionResult(false, null, result.Error);
            }
        }
        catch (Exception ex)
        {
            SetExecutionResult(false, null, $"执行异常: {ex.Message}");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public ExpressionItemViewModel()
    {
        // Update collapsed text when variables change
        VariableList.CollectionChanged += VariableList_CollectionChanged;
    }

    private void VariableList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCollapsedVariableText();
        
        // Subscribe to new variables' property changes
        if (e.NewItems != null)
        {
            foreach (VariableItemViewModel variable in e.NewItems)
            {
                variable.PropertyChanged += (s, args) => UpdateCollapsedVariableText();
            }
        }
    }

    partial void OnShowVariablesChanged(bool value)
    {
        UpdateCollapsedVariableText();
    }

    /// <summary>
    /// Update collapsed variable text for simplified display
    /// </summary>
    private void UpdateCollapsedVariableText()
    {
        if (!ShowVariables || VariableList.Count == 0)
        {
            CollapsedVariableText = string.Empty;
            return;
        }

        // Generate simplified text: "变量: x (Int), y (String), ..."
        var varTexts = VariableList.Select(v => $"{v.VarName} ({v.VarType})");
        CollapsedVariableText = $"变量: {string.Join(", ", varTexts)}";
    }

    /// <summary>
    /// Initialize from VariableClass list and expression string
    /// </summary>
    public void Initialize(List<VariableClass> variables, string expression)
    {
        // Clear existing variables
        VariableList.Clear();

        // Add new variables
        foreach (var variable in variables)
        {
            var viewModel = new VariableItemViewModel(variable);
            viewModel.PropertyChanged += (s, e) => UpdateCollapsedVariableText();
            VariableList.Add(viewModel);
        }

        Expression = expression;
        UpdateCollapsedVariableText();
    }

    /// <summary>
    /// Initialize with execution result
    /// </summary>
    public void SetExecutionResult(bool success, object? result, string? error = null)
    {
        ExecutionSuccess = success;
        ShowExecutionResult = true;

        if (success && result != null)
        {
            try
            {
                ExecutionResult = System.Text.Json.JsonSerializer.Serialize(
                    result,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                ExecutionError = string.Empty;
            }
            catch (Exception ex)
            {
                ExecutionResult = result.ToString() ?? string.Empty;
                ExecutionError = string.Empty;
            }
        }
        else
        {
            ExecutionResult = error ?? "执行失败";
            ExecutionError = error ?? "执行失败";
        }
    }
}

