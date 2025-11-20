using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Quicker.Common.Vm.Expression;
using Quicker.Domain.Actions.X.Storage;
using Quicker.Public.Actions;
using Quicker.View;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Wrapper for Quicker Code Editor that implements IExpressionAgentToolHandler
/// Provides integration between expression agent and Quicker's code editor window
/// </summary>
public class CodeEditorWrapper : IExpressionAgentToolHandler
{
    private readonly CodeEditorWindow _codeEditorWindow;
    private readonly List<ActionVariable> _sourceVarList;
    private readonly ObservableCollection<ExpressionInputParam> _variableList;
    private readonly object _variablesLock = new object();

    /// <summary>
    /// Current expression code (C# code with {variableName} format)
    /// </summary>
    public string Expression
    {
        get
        {
            if (_codeEditorWindow == null || !_codeEditorWindow.IsLoaded)
            {
                return string.Empty;
            }
            
            return _codeEditorWindow.Text ?? string.Empty;
        }
        set
        {
            if (_codeEditorWindow == null)
            {
                return;
            }
            
            _codeEditorWindow.Text = value ?? string.Empty;
        }
    }

    /// <summary>
    /// Get the unique identifier for this wrapper (based on GetHashCode)
    /// Uses the default object reference hash code
    /// </summary>
    public string WrapperId => GetHashCode().ToString();

    /// <summary>
    /// Get the window handle of the code editor window
    /// </summary>
    public IntPtr WindowHandle
    {
        get
        {
            if (_codeEditorWindow == null || !_codeEditorWindow.IsLoaded)
            {
                return IntPtr.Zero;
            }
            
            return new System.Windows.Interop.WindowInteropHelper(_codeEditorWindow).Handle;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public CodeEditorWrapper()
    {
        _sourceVarList = new List<ActionVariable>();
        _codeEditorWindow = new CodeEditorWindow(_sourceVarList, true, "")
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = true
        };

        // Get variable list from window
        var varListBox = (System.Windows.Controls.ListBox)_codeEditorWindow.FindName("LbVariables");
        if (varListBox != null && varListBox.ItemsSource != null)
        {
            // Try to cast to ObservableCollection or create new one
            if (varListBox.ItemsSource is ObservableCollection<ExpressionInputParam> obsCollection)
            {
                _variableList = obsCollection;
            }
            else
            {
                // If it's a different type (like FullyObservableCollection), create a new ObservableCollection
                _variableList = new ObservableCollection<ExpressionInputParam>();
                if (varListBox.ItemsSource is System.Collections.IEnumerable enumerable)
                {
                    foreach (ExpressionInputParam item in enumerable)
                    {
                        _variableList.Add(item);
                    }
                }
            }
        }
        else
        {
            _variableList = new ObservableCollection<ExpressionInputParam>();
        }
    }

    /// <summary>
    /// Show the code editor window
    /// </summary>
    public void Show()
    {
        if (_codeEditorWindow != null)
        {
            _codeEditorWindow.Show();
            _codeEditorWindow.Activate();
        }
    }

    /// <summary>
    /// Close the code editor window
    /// </summary>
    public void Close()
    {
        if (_codeEditorWindow != null)
        {
            _codeEditorWindow.Close();
        }
    }

    /// <summary>
    /// Set or update a variable
    /// </summary>
    public void SetVariable(VariableClass variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (_codeEditorWindow == null)
        {
            return;
        }

        lock (_variablesLock)
        {
            // Convert VariableClass to ActionVariable
            var actionVar = ConvertToActionVariable(variable);
            
            // Check if variable already exists
            var existingVar = _sourceVarList.FirstOrDefault(v => v.Key == variable.VarName);
            if (existingVar != null)
            {
                // Update existing variable
                var index = _sourceVarList.IndexOf(existingVar);
                _sourceVarList[index] = actionVar;
                
                // Update ExpressionInputParam if exists
                var existingParam = _variableList.FirstOrDefault(p => p.Key == variable.VarName);
                if (existingParam != null)
                {
                    existingParam.VarType = ConvertToVarType(variable.VarType);
                    existingParam.SampleValue = ConvertDefaultValueToString(variable.DefaultValue);
                }
                else
                {
                    // Add new ExpressionInputParam
                    _variableList.Add(ConvertToExpressionInputParam(variable));
                }
            }
            else
            {
                // Add new variable
                _sourceVarList.Add(actionVar);
                _variableList.Add(ConvertToExpressionInputParam(variable));
            }
        }
    }

    /// <summary>
    /// Get a specific variable by name
    /// </summary>
    public VariableClass? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (_codeEditorWindow == null)
        {
            return null;
        }

        lock (_variablesLock)
        {
            var actionVar = _sourceVarList.FirstOrDefault(v => v.Key == name);
            if (actionVar == null)
            {
                return null;
            }

            return ConvertToVariableClass(actionVar);
        }
    }

    /// <summary>
    /// Get all variables
    /// </summary>
    public List<VariableClass> GetAllVariables()
    {
        if (_codeEditorWindow == null)
        {
            return new List<VariableClass>();
        }

        lock (_variablesLock)
        {
            return _sourceVarList.Select(ConvertToVariableClass).ToList();
        }
    }

    /// <summary>
    /// Test an expression for syntax and execution
    /// </summary>
    public async Task<ExpressionResult> TestExpression(string expression, List<VariableClass>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new ExpressionResult
            {
                Success = false,
                Error = "Expression cannot be empty."
            };
        }

        // Use provided variables, or fall back to current variables
        List<VariableClass> variablesToUse = variables ?? GetAllVariables();

        // For now, return a placeholder result
        // In a full implementation, this would call a Roslyn service or Quicker's expression executor
        return new ExpressionResult
        {
            Success = false,
            Error = "Expression testing not yet implemented in CodeEditorWrapper. Use ExpressionExecutor for testing."
        };
    }

    #region Conversion Methods

    /// <summary>
    /// Convert VariableClass to ActionVariable
    /// </summary>
    private ActionVariable ConvertToActionVariable(VariableClass variable)
    {
        return new ActionVariable
        {
            Key = variable.VarName,
            Type = ConvertToVarType(variable.VarType),
            DefaultValue = ConvertDefaultValueToString(variable.DefaultValue),
            Desc = ""
        };
    }

    /// <summary>
    /// Convert ActionVariable to VariableClass
    /// </summary>
    private VariableClass ConvertToVariableClass(ActionVariable actionVar)
    {
        return new VariableClass
        {
            VarName = actionVar.Key,
            VarType = ConvertToVariableType(actionVar.Type),
            DefaultValue = ConvertStringToDefaultValue(actionVar.DefaultValue, actionVar.Type)
        };
    }

    /// <summary>
    /// Convert VariableClass to ExpressionInputParam
    /// </summary>
    private ExpressionInputParam ConvertToExpressionInputParam(VariableClass variable)
    {
        return new ExpressionInputParam
        {
            VarType = ConvertToVarType(variable.VarType),
            Key = variable.VarName,
            SampleValue = ConvertDefaultValueToString(variable.DefaultValue),
            Description = ""
        };
    }

    /// <summary>
    /// Convert VariableType to VarType
    /// Uses string-based conversion to handle enum differences
    /// </summary>
    private VarType ConvertToVarType(VariableType variableType)
    {
        // Use string-based conversion to handle potential enum differences
        var typeName = variableType.ToString();
        try
        {
            return (VarType)Enum.Parse(typeof(VarType), typeName, ignoreCase: true);
        }
        catch
        {
            // If parsing fails, try to get the first enum value as fallback
            var enumValues = Enum.GetValues(typeof(VarType));
            if (enumValues.Length > 0)
            {
                return (VarType)enumValues.GetValue(0)!;
            }
            throw new InvalidOperationException($"Cannot convert VariableType.{variableType} to VarType");
        }
    }

    /// <summary>
    /// Convert VarType to VariableType
    /// Uses string-based conversion to handle enum differences
    /// </summary>
    private VariableType ConvertToVariableType(VarType varType)
    {
        // Use string-based conversion to handle potential enum differences
        var typeName = varType.ToString();
        try
        {
            return (VariableType)Enum.Parse(typeof(VariableType), typeName, ignoreCase: true);
        }
        catch
        {
            // If parsing fails, default to String
            return VariableType.String;
        }
    }

    /// <summary>
    /// Convert default value to string representation
    /// </summary>
    private string ConvertDefaultValueToString(object? defaultValue)
    {
        if (defaultValue == null)
        {
            return string.Empty;
        }

        if (defaultValue is string str)
        {
            return str;
        }

        if (defaultValue is System.Collections.IEnumerable enumerable && !(defaultValue is string))
        {
            return string.Join(",", enumerable.Cast<object>());
        }

        return defaultValue.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Convert string to default value based on VarType
    /// Uses string comparison to determine the type
    /// </summary>
    private object ConvertStringToDefaultValue(string? value, VarType varType)
    {
        var typeName = varType.ToString();
        
        if (string.IsNullOrEmpty(value))
        {
            return typeName switch
            {
                "String" => string.Empty,
                "Int" => 0,
                "Double" => 0.0,
                "Bool" => false,
                "DateTime" => DateTime.MinValue,
                "ListString" => new List<string>(),
                "Dictionary" => new Dictionary<string, object>(),
                "Object" => new object(),
                _ => string.Empty
            };
        }

        return typeName switch
        {
            "String" => value,
            "Int" => int.TryParse(value, out var intVal) ? intVal : 0,
            "Double" => double.TryParse(value, out var doubleVal) ? doubleVal : 0.0,
            "Bool" => bool.TryParse(value, out var boolVal) && boolVal,
            "DateTime" => DateTime.TryParse(value, out var dateVal) ? dateVal : DateTime.MinValue,
            "ListString" => value.Split(',').ToList(),
            "Dictionary" => new Dictionary<string, object>(),
            "Object" => value,
            _ => value
        };
    }

    #endregion
}

