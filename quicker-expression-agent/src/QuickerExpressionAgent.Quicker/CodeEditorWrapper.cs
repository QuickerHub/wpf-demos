using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;
using Quicker.Common.Vm.Expression;
using Quicker.Domain.Actions.X.Storage;
using Quicker.Public.Actions;
using Quicker.Public.Interfaces;
using Quicker.Public;
using Quicker.Utilities._3rd;
using Quicker.Utilities;
using Quicker.View.Controls;
using Quicker.View;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Quicker.Public.Extensions;
using System.Collections.Concurrent;
using Z.Expressions;

namespace QuickerExpressionAgent.Quicker;

public class CodeEditorWrapper
{
    public CodeEditorWindow TheWindow { get; }
    private readonly ListBox _theVarListBox;
    private readonly FullyObservableCollection<ExpressionInputParam> _variableList = null!; // Initialized in constructor
    private readonly ICollection<ActionVariable> _sourceVarList;
    private readonly TextEditor _textEditor;
    private readonly ContextMenu _theListBoxMenu;
    private readonly IActionContext? _context;

    /// <summary>
    /// Constructor with optional existing CodeEditorWindow and context
    /// </summary>
    public CodeEditorWrapper(CodeEditorWindow? existingWindow = null, IActionContext? context = null)
    {
        _context = context;

        if (existingWindow != null)
        {
            // Use existing window
            TheWindow = existingWindow;
            
            // Get the first private readonly ICollection<ActionVariable> property from the window using reflection
            _sourceVarList = GetActionVariableCollectionFromWindow(existingWindow) ?? new List<ActionVariable>();
        }
        else
        {
            // Initialize _sourceVarList first (readonly field must be initialized once)
            _sourceVarList = new List<ActionVariable>();
            
            // Create new window
            TheWindow = new CodeEditorWindow(_sourceVarList, true, "")
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true
            };
        }

        // Access FindName directly in constructor
        _textEditor = (TextEditor)TheWindow.FindName("textEditor");

        #region ForListBox
        _theVarListBox = (ListBox)TheWindow.FindName("LbVariables");
        _variableList = (FullyObservableCollection<ExpressionInputParam>?)_theVarListBox.ItemsSource 
            ?? new FullyObservableCollection<ExpressionInputParam>();

        _theListBoxMenu = new ContextMenu();
        CreateContextMenu();
        _theVarListBox.ContextMenu = _theListBoxMenu;
        _theVarListBox.SetValue(ListBox.AllowDropProperty, true);
        _theVarListBox.SetValue(GongSolutions.Wpf.DragDrop.DragDrop.IsDragSourceProperty, true);
        _theVarListBox.SetValue(GongSolutions.Wpf.DragDrop.DragDrop.IsDropTargetProperty, true);

        #endregion

        if (CheckIsInQuicker() && string.IsNullOrEmpty(GetText()))
        {
            SetText("$=");
        }
    }

    /// <summary>
    /// Get the first private readonly ICollection<ActionVariable> property/field from the window using reflection
    /// </summary>
    private static ICollection<ActionVariable>? GetActionVariableCollectionFromWindow(CodeEditorWindow window)
    {
        if (window == null) return null;

        var windowType = window.GetType();
        var targetType = typeof(ICollection<ActionVariable>);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        // Check properties first
        var property = windowType.GetProperties(flags)
            .FirstOrDefault(p => targetType.IsAssignableFrom(p.PropertyType));
        
        if (property != null)
        {
            try
            {
                return property.GetValue(window) as ICollection<ActionVariable>;
            }
            catch { }
        }

        // Check fields if property not found
        var field = windowType.GetFields(flags)
            .FirstOrDefault(f => targetType.IsAssignableFrom(f.FieldType));
        
        if (field != null)
        {
            try
            {
                return field.GetValue(window) as ICollection<ActionVariable>;
            }
            catch { }
        }

        return null;
    }

    public static bool CheckIsInQuicker() => Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";

    /// <summary>
    /// Get context for state storage
    /// </summary>
    public IActionContext? GetContext()
    {
        return _context;
    }

    public EvalContext EvalContext => TheWindow.EvalContext;

    /// <summary>
    /// Get text from text editor (supports undo/redo)
    /// </summary>
    public string GetText()
    {
        return _textEditor?.Document.Text ?? string.Empty;
    }

    /// <summary>
    /// Set text in text editor (supports undo/redo)
    /// </summary>
    public void SetText(string text)
    {
        _textEditor?.Document.Text = text ?? string.Empty;
    }

    /// <summary>
    /// Set variable list (ActionVariable is the model, ExpressionInputParam is the view model)
    /// Both are stored separately and synchronized by Key
    /// </summary>
    public void SetVarList(List<ActionVariable> variables, List<ExpressionInputParam>? inputParams = null)
    {
        if (variables == null) return;

        // Set variables using Clear + Add
        _sourceVarList.Clear();
        
        // Use AddRange if it's a List, otherwise add items one by one
        if (_sourceVarList is List<ActionVariable> list)
        {
            list.AddRange(variables);
        }
        else
        {
            foreach (var variable in variables)
            {
                _sourceVarList.Add(variable);
            }
        }

        // Set input params using Clear + Reset (only if list is initialized)
        if (_variableList != null)
        {
            _variableList.Clear();
            if (inputParams != null && inputParams.Count > 0)
            {
                _variableList.Reset(inputParams);
            }
        }
    }

    /// <summary>
    /// Set or update a single variable
    /// </summary>
    public void SetVariable(ActionVariable variable)
    {
        if (variable == null) return;

        var existingVar = _sourceVarList.FirstOrDefault(v => v.Key == variable.Key);
        if (existingVar != null)
        {
            // Update existing variable
            // Use IList<T> if available for index access, otherwise use Remove + Add
            if (_sourceVarList is IList<ActionVariable> list)
            {
                var index = list.IndexOf(existingVar);
                list[index] = variable;
            }
            else
            {
                _sourceVarList.Remove(existingVar);
                _sourceVarList.Add(variable);
            }

            // Update ExpressionInputParam if exists
            var existingParam = _variableList.FirstOrDefault(p => p.Key == variable.Key);
            if (existingParam != null)
            {
                existingParam.VarType = variable.Type;
                existingParam.SampleValue = variable.DefaultValue;
            }
            else
            {
                // Add new ExpressionInputParam
                _variableList.Add(new ExpressionInputParam
                {
                    VarType = variable.Type,
                    Key = variable.Key,
                    SampleValue = variable.DefaultValue,
                    Description = variable.Desc ?? ""
                });
            }
        }
        else
        {
            // Add new variable
            _sourceVarList.Add(variable);
            _variableList.Add(new ExpressionInputParam
            {
                VarType = variable.Type,
                Key = variable.Key,
                SampleValue = variable.DefaultValue,
                Description = variable.Desc ?? ""
            });
        }
    }

    /// <summary>
    /// Get a specific variable by name
    /// </summary>
    public ActionVariable? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _sourceVarList.FirstOrDefault(v => v.Key == name);
    }

    /// <summary>
    /// Get all variables
    /// </summary>
    public List<ActionVariable> GetAllVariables()
    {
        return _sourceVarList?.ToList() ?? new List<ActionVariable>();
    }

    /// <summary>
    /// Get the unique identifier for this wrapper (based on GetHashCode)
    /// </summary>
    public string WrapperId => GetHashCode().ToString();

    /// <summary>
    /// Get the window handle of the code editor window
    /// </summary>
    public IntPtr WindowHandle
    {
        get
        {
            if (TheWindow == null || !TheWindow.IsLoaded)
            {
                return IntPtr.Zero;
            }

            return new System.Windows.Interop.WindowInteropHelper(TheWindow).Handle;
        }
    }

    /// <summary>
    /// Show the code editor window
    /// </summary>
    public void Show()
    {
        if (TheWindow != null)
        {
            TheWindow.Show();
            TheWindow.Activate();
        }
    }

    /// <summary>
    /// Close the code editor window
    /// </summary>
    public void Close()
    {
        if (TheWindow != null)
        {
            TheWindow.Close();
        }
    }

    public void CreateContextMenu()
    {
        _theListBoxMenu.Items.Add(CreateMenu("删除", (s, e) =>
        {
            if (_theVarListBox.SelectedItem != null)
            {
                RemoveVariable((ExpressionInputParam)_theVarListBox.SelectedItem);
            }
        }));
        _theListBoxMenu.Items.Add(CreateMenu("清空", (s, e) =>
        {
            var result = MessageBox.Show("确认要清空么", "确认", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;
            _variableList.Clear();
        }));
    }

    private MenuItem CreateMenu(string header, RoutedEventHandler clicked)
    {
        var menu = new MenuItem() { Header = header };
        menu.Click += clicked;
        return menu;
    }

    private void RemoveVariable(ExpressionInputParam inputParam)
    {
        _variableList.Remove(inputParam);
        var key = inputParam.Key;
        var vari = _sourceVarList.First(x => x.Key == key);
        _sourceVarList.Remove(vari);
    }

    private bool AddVariable(VarType type, string key)
    {
        if (_sourceVarList.Where(x => x.Key == key).Count() > 0)
        {
            AppHelper.ShowInformation("变量已存在");
            var vari = _sourceVarList.First(x => x.Key == key);
            if (_variableList.Where(x => x.Key == key).Count() == 0)
            {
                _variableList.Add(new ExpressionInputParam()
                {
                    VarType = vari.Type,
                    Key = key,
                    SampleValue = "",
                    Description = ""
                });
            }
            return false;
        }
        try
        {
            Z.Expressions.Eval.Execute("string " + key);
        }
        catch
        {
            AppHelper.ShowInformation("变量名不合法");
            return false;
        }
        _variableList.Add(new ExpressionInputParam()
        {
            VarType = type,
            Key = key,
            SampleValue = "",
            Description = ""
        });
        _sourceVarList.Add(new ActionVariable()
        {
            Key = key,
            Type = type,
            DefaultValue = "",
            Desc = ""
        });
        _theVarListBox.ScrollIntoView(_theVarListBox.Items[_theVarListBox.Items.Count - 1]);
        return true;
    }
}
