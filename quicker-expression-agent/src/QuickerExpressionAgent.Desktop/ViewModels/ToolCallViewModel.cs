using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

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
    /// Function call ID (for tracking)
    /// </summary>
    [ObservableProperty]
    private string _functionCallId = string.Empty;

    /// <summary>
    /// Function arguments (JSON string)
    /// </summary>
    [ObservableProperty]
    private string _arguments = string.Empty;

    /// <summary>
    /// Formatted arguments (indented JSON)
    /// </summary>
    private string? _formattedArguments;

    /// <summary>
    /// Function result (JSON string)
    /// </summary>
    [ObservableProperty]
    private string? _result;

    /// <summary>
    /// Formatted result (indented JSON)
    /// </summary>
    private string? _formattedResult;


    /// <summary>
    /// Whether output result is available
    /// </summary>
    [ObservableProperty]
    private bool _hasOutputResult = false;

    /// <summary>
    /// Markdown content for displaying tool call information
    /// </summary>
    [ObservableProperty]
    private string _markdownContent = string.Empty;

    /// <summary>
    /// Whether the message is expanded
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

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
        // UpdateMarkdownContent will be called by OnFunctionNameChanged
    }

    /// <summary>
    /// Set output result
    /// </summary>
    public void SetOutputResult(string result)
    {
        OutputResult = result;
        HasOutputResult = !string.IsNullOrWhiteSpace(result);
        // UpdateMarkdownContent will be called by OnResultChanged
    }

    partial void OnFunctionNameChanged(string value)
    {
        UpdateMarkdownContent();
    }

    partial void OnArgumentsChanged(string value)
    {
        // Format JSON with indentation
        _formattedArguments = FormatJson(value);
        UpdateMarkdownContent();
    }

    partial void OnResultChanged(string? value)
    {
        // Format JSON with indentation
        _formattedResult = FormatJson(value);
        UpdateMarkdownContent();
        
        // Auto-collapse when result is available
        if (!string.IsNullOrWhiteSpace(value))
        {
            IsExpanded = false;
        }
    }

    /// <summary>
    /// Format JSON string with indentation
    /// </summary>
    private string? FormatJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            // Try to parse and format as JSON
            using var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Allow all characters without escaping
            };
            return JsonSerializer.Serialize(doc.RootElement, options);
        }
        catch
        {
            // If not valid JSON, return original string
            return json;
        }
    }

    /// <summary>
    /// Check if arguments string is empty (null, whitespace, or empty JSON object)
    /// </summary>
    private bool IsEmptyArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return true;

        // Check if it's an empty JSON object
        var trimmed = arguments.Trim();
        if (trimmed == "{}" || trimmed == "null")
            return true;

        // Try to parse as JSON and check if it's an empty object
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return !doc.RootElement.EnumerateObject().Any();
            }
            return false;
        }
        catch
        {
            // If not valid JSON, treat as non-empty
            return false;
        }
    }

    partial void OnInputParametersChanged(ObservableCollection<ToolCallParameterViewModel> value)
    {
        UpdateMarkdownContent();
    }

    private void UpdateMarkdownContent()
    {
        var markdown = new System.Text.StringBuilder();
        
        // Function name (small text, not heading)
        if (!string.IsNullOrWhiteSpace(FunctionName))
        {
            markdown.AppendLine($"**函数名**: {FunctionName}");
            markdown.AppendLine();
        }

        // Description
        if (!string.IsNullOrWhiteSpace(Description))
        {
            markdown.AppendLine(Description);
            markdown.AppendLine();
        }

        // Arguments section - only show if not empty
        if (!IsEmptyArguments(Arguments))
        {
            markdown.AppendLine("**输入参数**:");
            markdown.AppendLine();
            markdown.AppendLine("```json");
            // Use formatted arguments if available, otherwise use original
            markdown.AppendLine(_formattedArguments ?? Arguments);
            markdown.AppendLine("```");
            markdown.AppendLine();
        }
        else if (InputParameters.Count > 0)
        {
            markdown.AppendLine("**输入参数**:");
            markdown.AppendLine();
            foreach (var param in InputParameters)
            {
                markdown.AppendLine($"- **{param.Title}**: {param.Description}");
            }
            markdown.AppendLine();
        }

        // Result section
        if (!string.IsNullOrWhiteSpace(Result))
        {
            markdown.AppendLine("**输出结果**:");
            markdown.AppendLine();
            markdown.AppendLine("```json");
            // Use formatted result if available, otherwise use original
            markdown.AppendLine(_formattedResult ?? Result);
            markdown.AppendLine("```");
        }
        else if (!string.IsNullOrWhiteSpace(OutputResult))
        {
            markdown.AppendLine("**输出结果**:");
            markdown.AppendLine();
            markdown.AppendLine("```");
            markdown.AppendLine(OutputResult);
            markdown.AppendLine("```");
        }

        MarkdownContent = markdown.ToString();
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

