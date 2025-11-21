using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Quicker.Common.Vm.Expression;
using Quicker.Domain.Actions.X.Storage;
using Quicker.Domain.Actions.X.Variables;
using Quicker.Public.Actions;
using Quicker.View;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Wrapper for Quicker Code Editor that implements IExpressionAgentToolHandler
/// Uses CodeEditorWrapper to manage CodeEditorWindow
/// </summary>
public class CodeEditorExpressionToolHandler : IExpressionAgentToolHandler
{
    private readonly CodeEditorWrapper _wrapper;

    /// <summary>
    /// Current expression code (C# code with {variableName} format)
    /// Note: CodeEditorWrapper stores expressions with $= prefix, so we need to add/remove it
    /// Uses _textEditor for undo/redo support
    /// </summary>
    public string Expression
    {
        get
        {
            var text = _wrapper.GetText();
            // Remove $= prefix if present
            if (text.StartsWith("$=", StringComparison.Ordinal))
            {
                return text.Substring(2);
            }
            return text;
        }
        set
        {
            var text = value ?? string.Empty;
            // Add $= prefix if not already present
            if (!string.IsNullOrEmpty(text) && !text.StartsWith("$=", StringComparison.Ordinal))
            {
                text = "$=" + text;
            }
            _wrapper.SetText(text);
        }
    }

    /// <summary>
    /// Get the unique identifier for this wrapper (based on GetHashCode)
    /// </summary>
    public string WrapperId => _wrapper.WrapperId;

    /// <summary>
    /// Get the window handle of the code editor window
    /// </summary>
    public IntPtr WindowHandle => _wrapper.WindowHandle;

    /// <summary>
    /// Constructor - creates a new CodeEditorWindow
    /// </summary>
    public CodeEditorExpressionToolHandler()
    {
        _wrapper = new CodeEditorWrapper();
    }

    /// <summary>
    /// Constructor with existing CodeEditorWindow
    /// </summary>
    public CodeEditorExpressionToolHandler(CodeEditorWindow existingWindow)
    {
        _wrapper = new CodeEditorWrapper(existingWindow);
    }

    /// <summary>
    /// Show the code editor window
    /// </summary>
    public void Show()
    {
        _wrapper.Show();
    }

    /// <summary>
    /// Close the code editor window
    /// </summary>
    public void Close()
    {
        _wrapper.Close();
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

        var actionVar = ConvertToActionVariable(variable);
        _wrapper.SetVariable(actionVar);
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

        var actionVar = _wrapper.GetVariable(name);
        return actionVar != null ? ConvertToVariableClass(actionVar) : null;
    }

    /// <summary>
    /// Get all variables
    /// </summary>
    public List<VariableClass> GetAllVariables()
    {
        return _wrapper.GetAllVariables().Select(ConvertToVariableClass).ToList();
    }

    /// <summary>
    /// Test an expression for syntax and execution
    /// </summary>
    public async Task<ExpressionResult> TestExpressionAsync(string expression, List<VariableClass>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new ExpressionResultError("Expression cannot be empty.");
        }

        // Merge variables: GetAllVariables() first, then override with provided variables
        var variablesDict = GetAllVariables().ToDictionary(v => v.VarName, v => v, StringComparer.Ordinal);
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                variablesDict[variable.VarName] = variable;
            }
        }

        try
        {
            // Replace placeholders and track used variables
            var code = expression;
            var usedVariables = new List<VariableClass>();
            var variablesValueDict = new Dictionary<string, object?>(StringComparer.Ordinal);
            
            foreach (var variable in variablesDict.Values)
            {
                var placeholder = $"{{{variable.VarName}}}";
                if (code.Contains(placeholder, StringComparison.Ordinal))
                {
                    usedVariables.Add(variable);
                    variablesValueDict[variable.VarName] = variable.GetDefaultValue();
                    code = code.Replace(placeholder, variable.VarName);
                }
            }

            // Execute expression
            object result = _wrapper.EvalContext.Execute(code, variablesValueDict);

            return new ExpressionResult(result, usedVariables);
        }
        catch (Exception ex)
        {
            return new ExpressionResultError(ex.Message);
        }
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
            DefaultValue = variable.DefaultValue, // Direct string assignment, no conversion needed
            Desc = ""
        };
    }

    /// <summary>
    /// Convert ActionVariable to VariableClass
    /// </summary>
    private VariableClass ConvertToVariableClass(ActionVariable actionVar)
    {
        var variable = new VariableClass
        {
            VarName = actionVar.Key,
            VarType = ConvertToVariableType(actionVar.Type)
        };
        // Convert ActionVariable.DefaultValue (object) to string
        var defaultValue = VariableHelper.ConvertVarDefaultValue(actionVar.Type, actionVar.DefaultValue);
        variable.SetDefaultValue(defaultValue);
        return variable;
    }

    /// <summary>
    /// Convert VariableType to VarType
    /// </summary>
    private VarType ConvertToVarType(VariableType variableType)
    {
        return variableType switch
        {
            VariableType.String => VarType.Text,
            VariableType.Int => VarType.Integer,
            VariableType.Double => VarType.Number,
            VariableType.Bool => VarType.Boolean,
            VariableType.DateTime => VarType.DateTime,
            VariableType.ListString => VarType.List,
            VariableType.Dictionary => VarType.Dict,
            VariableType.Object => VarType.Object,
            _ => VarType.Text // Default fallback
        };
    }

    /// <summary>
    /// Convert VarType to VariableType
    /// </summary>
    private VariableType ConvertToVariableType(VarType varType)
    {
        return varType switch
        {
            VarType.Text => VariableType.String,
            VarType.Integer => VariableType.Int,
            VarType.Number => VariableType.Double,
            VarType.Boolean => VariableType.Bool,
            VarType.DateTime => VariableType.DateTime,
            VarType.List => VariableType.ListString,
            VarType.Dict => VariableType.Dictionary,
            VarType.Object => VariableType.Object,
            _ => VariableType.String // Default fallback
        };
    }

    #endregion
}
