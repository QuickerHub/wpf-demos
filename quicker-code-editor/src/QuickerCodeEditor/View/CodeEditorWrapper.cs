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
using QuickerCodeEditor.View.CodeEditor;
using Z.Expressions;

namespace QuickerCodeEditor.View;

/// <summary>
/// Reflection helper extensions
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Get the first field or property of type T from the object using reflection
    /// </summary>
    public static T? GetField<T>(this object obj) where T : class
    {
        if (obj == null) return null;

        var objType = obj.GetType();
        var targetType = typeof(T);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        // Check properties first
#pragma warning disable IL2070, IL2075 // Reflection is intentional here - accessing properties/fields dynamically
        var property = objType.GetProperties(flags)
            .FirstOrDefault(p => targetType.IsAssignableFrom(p.PropertyType));
#pragma warning restore IL2070, IL2075

        if (property != null)
        {
            try
            {
                return property.GetValue(obj) as T;
            }
            catch { }
        }

        // Check fields if property not found
#pragma warning disable IL2070, IL2075 // Reflection is intentional here - accessing properties/fields dynamically
        var field = objType.GetFields(flags)
            .FirstOrDefault(f => targetType.IsAssignableFrom(f.FieldType));
#pragma warning restore IL2070, IL2075
        
        if (field != null)
        {
            try
            {
                return field.GetValue(obj) as T;
            }
            catch { }
        }

        return null;
    }
}

public class CodeEditorWrapper
{
    public Quicker.View.CodeEditorWindow TheWindow { get; }
    private readonly ListBox _theVarListBox;
    private readonly FullyObservableCollection<ExpressionInputParam> _variableList;
    private readonly List<ActionVariable> _sourceVarList;
    private readonly TextEditor _textEditor;
    private readonly ContextMenu _theListBoxMenu;
    private readonly CodeEditorState _state;
    private WindowAttachedPopup? _attachedPopup;
    private readonly IActionContext? _context;
    private CodeEditorStateControl? _stateControl;

    /// <summary>
    /// Constructor with context and optional state
    /// </summary>
    public CodeEditorWrapper(IActionContext? context = null, CodeEditorState? state = null)
    {
        _context = context;
        _state = state ?? CodeEditorState.CreateDefault();

        // Initialize _sourceVarList first (readonly field must be initialized once)
        _sourceVarList = new List<ActionVariable>();
        
        // Create new window
        TheWindow = new Quicker.View.CodeEditorWindow(_sourceVarList, true, "")
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = true
        };

        // Access FindName directly in constructor
        _textEditor = (TextEditor)TheWindow.FindName("textEditor");

        #region ForListBox
        _theVarListBox = (ListBox)TheWindow.FindName("LbVariables");
        _variableList = (FullyObservableCollection<ExpressionInputParam>)_theVarListBox.ItemsSource;

        _theListBoxMenu = new ContextMenu();
        CreateContextMenu();
        _theVarListBox.ContextMenu = _theListBoxMenu;
        _theVarListBox.SetValue(ListBox.AllowDropProperty, true);
        _theVarListBox.SetValue(GongSolutions.Wpf.DragDrop.DragDrop.IsDragSourceProperty, true);
        _theVarListBox.SetValue(GongSolutions.Wpf.DragDrop.DragDrop.IsDropTargetProperty, true);

        #endregion

        TheWindow.Loaded += (s, e) =>
        {
            // Set initial state directly
            SetText(_state.Expression);
            SetVarList(_state.Variables, _state.InputParams);

            // Create popup after window is loaded
            CreateAttachedPopup();
        };

        // Save state when window closes
        TheWindow.Closed += (s, e) =>
        {
            if (_stateControl != null && _context != null)
            {
                // Save current state to selected state before closing
                if (_stateControl.ViewModel.SelectedState != null)
                {
                    var currentState = GetState();
                    var selectedState = _stateControl.ViewModel.SelectedState;
                    
                    // Check if there are any changes
                    if (!CodeEditorState.AreContentEqual(selectedState, currentState))
                    {
                        selectedState.CopyContentFrom(currentState);
                        selectedState.UpdateTime = DateTime.Now;
                    }
                }
                
                // Save states list (handled in ViewModel)
                _stateControl.ViewModel.SaveStatesList();
            }
        };
    }

    private void CreateAttachedPopup()
    {
        _attachedPopup = new WindowAttachedPopup
        {
            TargetWindow = TheWindow,
            WindowPlacement = WindowPlacement.Left,
            OffsetX = 10,
            OffsetY = 0,
            Width = 300,
            Height = 400,
            IsOpen = false,
            StaysOpen = true
        };

        var border = new System.Windows.Controls.Border
        {
            Background = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("RegionBrush"),
            BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(4)
        };

        // Create CodeEditorStateControl
        _stateControl = new CodeEditorStateControl();
        
        // Set CodeEditorWrapper so selection changes will update the editor
        _stateControl.ViewModel.CodeEditorWrapper = this;
        
        // Load states list (handled in ViewModel)
        _stateControl.ViewModel.LoadStatesList();

        border.Child = _stateControl;
        _attachedPopup.Child = border;

        // Open popup if window is already active
        if (TheWindow.IsActive)
        {
            _attachedPopup.IsOpen = true;
        }
    }


    /// <summary>
    /// Get current state
    /// </summary>
    public CodeEditorState GetState()
    {
        return new CodeEditorState
        {
            Name = _state.Name,
            Expression = TheWindow.IsLoaded ? TheWindow.Text : _state.Expression,
            Variables = [.. _sourceVarList],
            InputParams = _variableList.ToList()
        };
    }

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
#if !DEBUG
        _state.Expression = text ?? CodeEditorState.DefaultExpression;
        _textEditor?.Document.Text = _state.Expression;
#else
        _textEditor?.Document.Text = text ?? string.Empty;
#endif
    }

    /// <summary>
    /// Create ExpressionInputParam from ActionVariable
    /// </summary>
    private ExpressionInputParam CreateInputParamFromVariable(ActionVariable variable, ExpressionInputParam? existingParam = null)
    {
        // Use existing param's SampleValue and Description if available (preserve user modifications)
        // Otherwise use values from ActionVariable directly (SampleValue accepts object type)
        return new ExpressionInputParam
        {
            VarType = variable.Type,
            Key = variable.Key,
            SampleValue = existingParam?.SampleValue ?? variable.DefaultValue,
            Description = existingParam?.Description ?? (variable.Desc ?? "")
        };
    }

    /// <summary>
    /// Set variable list (ActionVariable is the model, ExpressionInputParam is the view model)
    /// Merge inputParams with variables during initialization
    /// </summary>
    public void SetVarList(List<ActionVariable> variables, List<ExpressionInputParam>? inputParams = null)
    {
        if (variables == null) return;

        // Set variables using Clear + AddRange - directly modify the list that window holds reference to
        _sourceVarList.Clear();
        _sourceVarList.AddRange(variables);

        // Merge inputParams with variables during initialization
        _variableList.Clear();
        var inputParamsDict = inputParams?.ToDictionary(p => p.Key, p => p) ?? new Dictionary<string, ExpressionInputParam>();
        foreach (var variable in _sourceVarList)
        {
            inputParamsDict.TryGetValue(variable.Key, out var inputParam);
            _variableList.Add(CreateInputParamFromVariable(variable, inputParam));
        }
    }

    /// <summary>
    /// Set state (calls SetText and SetVarList)
    /// </summary>
    public void SetState(CodeEditorState state)
    {
        if (state == null) return;

        _state.Name = state.Name ?? "";
        SetText(state.Expression);
        SetVarList(state.Variables, state.InputParams);
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
        if (_sourceVarList.Any(x => x.Key == key))
        {
            AppHelper.ShowInformation("变量已存在");
            var vari = _sourceVarList.First(x => x.Key == key);
            if (!_variableList.Any(x => x.Key == key))
            {
                _variableList.Add(new ExpressionInputParam()
                {
                    VarType = vari.Type,
                    Key = key,
                    SampleValue = vari.DefaultValue,
                    Description = vari.Desc ?? ""
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
        // Add to both lists
        var actionVar = new ActionVariable()
        {
            Key = key,
            Type = type,
            DefaultValue = "",
            Desc = ""
        };
        _sourceVarList.Add(actionVar);
        _variableList.Add(new ExpressionInputParam()
        {
            VarType = type,
            Key = key,
            SampleValue = "",
            Description = ""
        });
        _theVarListBox.ScrollIntoView(_theVarListBox.Items[_theVarListBox.Items.Count - 1]);
        return true;
    }

}
