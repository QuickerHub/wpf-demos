using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Quicker.Common.Vm.Expression;
using Quicker.Domain.Actions.X.Storage;

namespace QuickerCodeEditor.View
{
    /// <summary>
    /// Code editor state model
    /// </summary>
    public partial class CodeEditorState : ObservableObject
    {
        public static readonly string DefaultExpression = "$=";

        /// <summary>
        /// Editor name
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = "";

        /// <summary>
        /// Expression text
        /// </summary>
        [ObservableProperty]
        public partial string Expression { get; set; } = DefaultExpression;

        /// <summary>
        /// Variable list (ActionVariable is the model)
        /// </summary>
        [ObservableProperty]
        public partial List<ActionVariable> Variables { get; set; } = new List<ActionVariable>();

        /// <summary>
        /// Input parameters list (ExpressionInputParam is the view model)
        /// </summary>
        [ObservableProperty]
        public partial List<ExpressionInputParam> InputParams { get; set; } = new List<ExpressionInputParam>();

        /// <summary>
        /// Last update time
        /// </summary>
        [ObservableProperty]
        public partial DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Create default state
        /// </summary>
        public static CodeEditorState CreateDefault(string? defaultExpression = null)
        {
            return new CodeEditorState
            {
                Expression = defaultExpression ?? DefaultExpression,
                Variables = new List<ActionVariable>(),
                InputParams = new List<ExpressionInputParam>()
            };
        }

        /// <summary>
        /// Copy content (Expression, Variables, InputParams) from another state
        /// </summary>
        public void CopyContentFrom(CodeEditorState source)
        {
            if (source == null) return;
            
            Expression = source.Expression;
            Variables = source.Variables;
            InputParams = source.InputParams;
        }

        /// <summary>
        /// Serialize content part (Expression, Variables, InputParams) to JSON string
        /// </summary>
        private static string SerializeContent(CodeEditorState state)
        {
            return JsonConvert.SerializeObject(new
            {
                state.Expression,
                state.Variables,
                state.InputParams
            });
        }

        /// <summary>
        /// Compare two states to check if they have the same content (Expression, Variables, InputParams)
        /// </summary>
        public static bool AreContentEqual(CodeEditorState? state1, CodeEditorState? state2)
        {
            if (state1 == null && state2 == null) return true;
            if (state1 == null || state2 == null) return false;

            return SerializeContent(state1) == SerializeContent(state2);
        }
    }
}

