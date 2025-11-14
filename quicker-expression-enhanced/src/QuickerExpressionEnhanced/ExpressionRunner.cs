using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Quicker.Public.Interfaces;
using Quicker.Domain.Actions.Runtime;
using Z.Expressions;
using Quicker.Domain.Actions;

namespace QuickerExpressionEnhanced
{
    /// <summary>
    /// Expression runner for Quicker actions
    /// Supports running expressions with variable substitution and UI thread execution
    /// </summary>
    public static class ExpressionRunner
    {
        /// <summary>
        /// Run expression with support for variable substitution and optional UI thread execution
        /// Supports direct variable assignment in expressions
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="eval">Eval context for expression evaluation</param>
        /// <param name="code">Expression code to execute</param>
        /// <param name="onUiThread">Whether to execute on UI thread</param>
        /// <param name="useArgs">Whether to parse and use arguments (default: false)</param>
        /// <returns>Expression execution result</returns>
        public static object? RunExpression(IActionContext context, EvalContext eval, string code, bool onUiThread, bool useArgs = false)
        {
            if (useArgs)
            {
                if (ParseArgs(context).GetAwaiter().GetResult())
                {
                    return null;
                }
            }

            // Remove "$=" prefix if present
            if (code.StartsWith("$="))
            {
                code = code.Substring(2);
            }

            // Replace variable placeholders {key} with variable names
            foreach (var key in context.GetVariables().Keys)
            {
                code = code.Replace($"{{{key}}}", key);
            }

            if (onUiThread)
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    return RunExp(context, eval, code);
                });
            }
            else
            {
                return RunExp(context, eval, code);
            }
        }

        /// <summary>
        /// Parse arguments and execute corresponding commands if parsing succeeds
        /// </summary>
        /// <param name="context">Action context</param>
        /// <returns>Whether argument parsing was successful</returns>
        private static async Task<bool> ParseArgs(IActionContext context)
        {
            var root = context.GetRootContext();
            var id = root.ActionId;
            var cmd = (string)root.GetVarValue("quicker_in_param");
            
            switch (cmd)
            {
                case "quicker vote":
                    // VoteAction functionality can be added here if needed
                    // await VoteAction(id);
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Execute expression with registered context variables
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="eval">Eval context</param>
        /// <param name="code">Code with variables already replaced</param>
        /// <returns>Expression execution result</returns>
        private static object RunExp(IActionContext context, EvalContext eval, string code)
        {
            var ac = (ActionExecuteContext)context;
            eval.RegisterLocalVariable("_context", context);
            eval.RegisterLocalVariable("_eval", eval);
            
            try
            {
                var res = eval.Execute(code, ac.CustomData);
                return res;
            }
            finally
            {
                eval.UnregisterLocalVariable("_context", "_eval");
            }
        }
    }
}

