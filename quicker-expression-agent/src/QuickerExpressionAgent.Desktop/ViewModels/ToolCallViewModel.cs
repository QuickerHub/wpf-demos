using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for displaying tool call information
/// </summary>
public partial class ToolCallViewModel : ObservableObject
{
    /// <summary>
    /// Function name (tool name)
    /// </summary>
    [ObservableProperty]
    private string _functionName = string.Empty;

    /// <summary>
    /// Function description (gray text, one line, auto-ellipsis)
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Input parameters list (key-value pairs)
    /// Format: "title: description"
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ToolCallParameterViewModel> _inputParameters = new();

    /// <summary>
    /// Output result text
    /// </summary>
    [ObservableProperty]
    private string _outputResult = string.Empty;

    /// <summary>
    /// Whether the control is expanded
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>
    /// Whether output result is available
    /// </summary>
    [ObservableProperty]
    private bool _hasOutputResult = false;

    /// <summary>
    /// Initialize from function name, description, and parameters
    /// </summary>
    public void Initialize(string functionName, string description, Dictionary<string, string>? parameters = null)
    {
        FunctionName = functionName;
        Description = description;
        
        InputParameters.Clear();
        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                InputParameters.Add(new ToolCallParameterViewModel
                {
                    Title = kvp.Key,
                    Description = kvp.Value
                });
            }
        }
    }

    /// <summary>
    /// Set output result
    /// </summary>
    public void SetOutputResult(string result)
    {
        OutputResult = result;
        HasOutputResult = !string.IsNullOrWhiteSpace(result);
    }
}

/// <summary>
/// ViewModel for a single tool call parameter
/// </summary>
public partial class ToolCallParameterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;
}

