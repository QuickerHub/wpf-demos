using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Quicker.Public.Extensions;
using QuickerCodeEditor.View;

namespace QuickerCodeEditor.View.CodeEditor
{
    /// <summary>
    /// ViewModel for CodeEditorStateControl
    /// </summary>
    public partial class CodeEditorStateControlViewModel : ObservableObject
    {
        /// <summary>
        /// Collection of CodeEditorState items
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<CodeEditorState> States { get; set; }

        /// <summary>
        /// The CodeEditorWrapper instance to apply state changes to
        /// </summary>
        [ObservableProperty]
        public partial CodeEditorWrapper? CodeEditorWrapper { get; set; }

        /// <summary>
        /// Currently selected CodeEditorState
        /// </summary>
        [ObservableProperty]
        public partial CodeEditorState? SelectedState { get; set; }

        /// <summary>
        /// Currently editing CodeEditorState (for rename)
        /// </summary>
        [ObservableProperty]
        public partial CodeEditorState? EditingState { get; set; }

        /// <summary>
        /// Temporary name for editing
        /// </summary>
        [ObservableProperty]
        public partial string? EditingName { get; set; }

        /// <summary>
        /// Event triggered when selection changes, allowing custom handling
        /// </summary>
        public event EventHandler<CodeEditorStateSelectionChangedEventArgs>? StateSelectionChanged;

        public CodeEditorStateControlViewModel()
        {
            States = new ObservableCollection<CodeEditorState>();
        }

        partial void OnSelectedStateChanged(CodeEditorState? oldValue, CodeEditorState? newValue)
        {
            // Save previous state before switching
            if (oldValue != null && CodeEditorWrapper != null)
            {
                var currentState = CodeEditorWrapper.GetState();
                
                // Check if there are any changes
                if (!CodeEditorState.AreContentEqual(oldValue, currentState))
                {
                    oldValue.CopyContentFrom(currentState);
                    oldValue.UpdateTime = DateTime.Now;
                    // Save states list when a state is saved
                    SaveStatesList();
                }
            }
            
            // Apply new state to CodeEditorWrapper if available
            if (newValue != null && CodeEditorWrapper != null)
            {
                CodeEditorWrapper.SetState(newValue);
            }
            
            // Raise custom event
            StateSelectionChanged?.Invoke(this, new CodeEditorStateSelectionChangedEventArgs
            {
                SelectedState = newValue
            });
        }

        /// <summary>
        /// Load states list from context
        /// </summary>
        public void LoadStatesList()
        {
            var context = CodeEditorWrapper?.GetContext();
            if (context != null && States != null)
            {
                States.Clear();
                
                var savedStates = context.ReadState("codeEditorStates", "[]").JsonToObject<List<CodeEditorState>>();
                if (savedStates != null && savedStates.Count > 0)
                {
                    foreach (var state in savedStates)
                    {
                        States.Add(state);
                    }
                }
                
                // Add test states if no saved states
                if (States.Count == 0)
                {
                    States.Add(new CodeEditorState
                    {
                        Name = "表达式1",
                        Expression = "$=1+1"
                    });
                    
                    States.Add(new CodeEditorState
                    {
                        Name = "表达式2",
                        Expression = "$=2+2"
                    });
                    
                    States.Add(new CodeEditorState
                    {
                        Name = "表达式3",
                        Expression = "$=3+3"
                    });
                }
            }
        }

        /// <summary>
        /// Save states list to context
        /// </summary>
        public void SaveStatesList()
        {
            var context = CodeEditorWrapper?.GetContext();
            if (context != null && States != null)
            {
                var states = States.ToList();
                context.WriteState("codeEditorStates", JsonConvert.SerializeObject(states));
            }
        }

        /// <summary>
        /// Add a new state
        /// </summary>
        [RelayCommand]
        private void AddState()
        {
            var newState = CodeEditorState.CreateDefault();
            newState.Name = $"表达式{States.Count + 1}";
            
            States.Insert(0, newState);
            SelectedState = newState;
            
            // Automatically start editing the new state's name
            EditingState = newState;
            EditingName = newState.Name;
            
            // Save states list after adding
            SaveStatesList();
        }

        /// <summary>
        /// Delete the selected state
        /// </summary>
        [RelayCommand]
        private void DeleteState(CodeEditorState? stateToDelete = null)
        {
            var state = stateToDelete ?? SelectedState;
            if (state != null && States.Contains(state))
            {
                // Record the index of the item to be deleted
                int deletedIndex = States.IndexOf(state);
                
                States.Remove(state);
                
                // Select the item at the same index (or the previous one if it was the last item)
                if (States.Count > 0)
                {
                    int targetIndex = deletedIndex < States.Count ? deletedIndex : States.Count - 1;
                    SelectedState = States[targetIndex];
                }
                else
                {
                    SelectedState = null;
                }
                
                // Save states list after deleting
                SaveStatesList();
            }
        }
    }

    /// <summary>
    /// Event arguments for CodeEditorState selection changed event
    /// </summary>
    public class CodeEditorStateSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The selected CodeEditorState
        /// </summary>
        public CodeEditorState? SelectedState { get; set; }
    }
}

