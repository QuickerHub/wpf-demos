using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Quicker.Public.Interfaces;
using Quicker.Domain.Actions.Runtime;
using Z.Expressions;
using Quicker.Domain.Actions;
using log4net;
using Quicker.Utilities;

namespace QuickerExpressionEnhanced
{
    /// <summary>
    /// Expression runner for Quicker actions
    /// Supports running expressions with variable substitution and UI thread execution
    /// </summary>
    public static class ExpressionRunner
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ExpressionRunner));
        /// <summary>
        /// Run expression with support for variable substitution and optional UI thread execution
        /// Supports direct variable assignment in expressions
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="eval">Eval context for expression evaluation</param>
        /// <param name="code">Expression code to execute</param>
        /// <param name="onUiThread">Whether to execute on UI thread</param>
        /// <param name="useArgs">Whether to parse and use arguments (default: false)</param>
        /// <param name="toAction">Whether to return an Action instead of executing immediately (default: false)</param>
        /// <returns>Expression execution result, or Action if toAction is true</returns>
        public static object? RunExpression(IActionContext context, EvalContext eval, string code, bool onUiThread, bool useArgs = false, bool toAction = false)
        {
            if (toAction)
            {
                // Return an Action that will execute the expression when invoked
                return new Action(() =>
                {
                    RunExpressionInternal(context, eval, code, onUiThread, useArgs);
                });
            }
            else
            {
                // Execute immediately and return result
                return RunExpressionInternal(context, eval, code, onUiThread, useArgs);
            }
        }

        /// <summary>
        /// Run expression with support for variable substitution and optional UI thread execution
        /// Overload that accepts object? context which will be converted to IActionContext if possible
        /// </summary>
        /// <param name="context">Context object (will be cast to IActionContext if possible)</param>
        /// <param name="eval">Eval context for expression evaluation</param>
        /// <param name="code">Expression code to execute</param>
        /// <param name="onUiThread">Whether to execute on UI thread</param>
        /// <param name="useArgs">Whether to parse and use arguments (default: false)</param>
        /// <param name="toAction">Whether to return an Action instead of executing immediately (default: false)</param>
        /// <returns>Expression execution result, or Action if toAction is true</returns>
        /// <exception cref="ArgumentException">Thrown when context is not null and cannot be cast to IActionContext</exception>
        public static object? RunExpression(object? context, EvalContext eval, string code, bool onUiThread, bool useArgs = false, bool toAction = false)
        {
            // Convert object? context to IActionContext
            IActionContext? actionContext = null;
            if (context != null)
            {
                if (context is IActionContext ac)
                {
                    actionContext = ac;
                }
                else
                {
                    throw new ArgumentException($"Context must be of type IActionContext, but got {context.GetType().Name}", nameof(context));
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(context), "Context cannot be null");
            }

            // Call the main RunExpression method
            return RunExpression(actionContext, eval, code, onUiThread, useArgs, toAction);
        }

        /// <summary>
        /// Internal method to run expression with support for variable substitution and optional UI thread execution
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="eval">Eval context for expression evaluation</param>
        /// <param name="code">Expression code to execute</param>
        /// <param name="onUiThread">Whether to execute on UI thread</param>
        /// <param name="useArgs">Whether to parse and use arguments</param>
        /// <returns>Expression execution result</returns>
        private static object? RunExpressionInternal(IActionContext context, EvalContext eval, string code, bool onUiThread, bool useArgs = false)
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
                    return RunExpWithMessage(context, eval, code);
                });
            }
            else
            {
                return RunExpWithMessage(context, eval, code);
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
        /// Execute expression with registered context variables and show error message on failure
        /// This method catches exceptions and shows error message instead of throwing
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="eval">Eval context</param>
        /// <param name="code">Code with variables already replaced</param>
        /// <returns>Expression execution result, or null if execution fails</returns>
        private static object? RunExpWithMessage(IActionContext context, EvalContext eval, string code)
        {
            try
            {
                return RunExp(context, eval, code);
            }
            catch (Exception ex)
            {
                // Log error when expression execution fails
                _log.Error($"Failed to execute expression. Code: {code}", ex);
                AppHelper.ShowWarning($"表达式执行失败: {ex.Message}\n代码: {code}");
                throw;
            }
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "Action context cannot be null");
            }

            if (eval == null)
            {
                throw new ArgumentNullException(nameof(eval), "Eval context cannot be null");
            }

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

