using System;
using System.Collections.Generic;
using System.Linq;
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

namespace QuickerCodeEditor.View;

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

        _sourceVarList = new List<ActionVariable>();
        TheWindow = new Quicker.View.CodeEditorWindow(_sourceVarList, true, "")
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = true
        };

        // Access FindName directly in constructor, like the working version
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
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = System.Windows.Media.Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5)
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
            InputParams = _variableList?.ToList() ?? new List<ExpressionInputParam>()
        };
    }

    /// <summary>
    /// Get context for state storage
    /// </summary>
    public IActionContext? GetContext()
    {
        return _context;
    }

    /// <summary>
    /// Set text
    /// </summary>
    public void SetText(string text)
    {
#if !DEBUG
        _state.Expression = text ?? CodeEditorState.DefaultExpression;
        TheWindow.Text = _state.Expression;
#endif
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
        _sourceVarList.AddRange(variables);

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
