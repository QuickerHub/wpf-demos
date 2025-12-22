using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Quicker.Public.Interfaces;
using Quicker.Domain.Actions.Runtime;
using Z.Expressions;
using Quicker.Domain.Actions;
using log4net;
using Quicker.Utilities;
using QuickerExpressionEnhanced.Parser;

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
        /// <param name="registrationCommands">Registration commands string (load, using, type commands)</param>
        /// <returns>Expression execution result, or Action if toAction is true</returns>
        public static object? RunExpression(IActionContext context, EvalContext eval, string code, bool onUiThread, bool useArgs = false, bool toAction = false, string? registrationCommands = null)
        {
            if (toAction)
            {
                // Return an Action that will execute the expression when invoked
                return new Action(() =>
                {
                    RunExpressionInternal(context, eval, code, onUiThread, useArgs, registrationCommands);
                });
            }
            else
            {
                // Execute immediately and return result
                return RunExpressionInternal(context, eval, code, onUiThread, useArgs, registrationCommands);
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
        /// <param name="registrationCommands">Registration commands string (load, using, type commands)</param>
        /// <returns>Expression execution result, or Action if toAction is true</returns>
        /// <exception cref="ArgumentException">Thrown when context is not null and cannot be cast to IActionContext</exception>
        public static object? RunExpression(object? context, EvalContext eval, string code, bool onUiThread, bool useArgs = false, bool toAction = false, string? registrationCommands = null)
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
            return RunExpression(actionContext, eval, code, onUiThread, useArgs, toAction, registrationCommands);
        }

        /// <summary>
        /// Internal method to run expression with support for variable substitution and optional UI thread execution
        /// </summary>
        /// <param name="context">Action context</param>
        /// <param name="eval">Eval context for expression evaluation</param>
        /// <param name="code">Expression code to execute</param>
        /// <param name="onUiThread">Whether to execute on UI thread</param>
        /// <param name="useArgs">Whether to parse and use arguments</param>
        /// <param name="registrationCommands">Registration commands string (load, using, type commands)</param>
        /// <returns>Expression execution result</returns>
        private static object? RunExpressionInternal(IActionContext context, EvalContext eval, string code, bool onUiThread, bool useArgs = false, string? registrationCommands = null)
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

            // Parse and execute registration commands from parameter first
            if (!string.IsNullOrWhiteSpace(registrationCommands))
            {
                var parseResult = RegistrationCommandParser.Parse(registrationCommands!, context);
                RegistrationCommandExecutor.Register(eval, parseResult.Commands);
            }

            // Parse registration commands from code
            var codeParseResult = RegistrationCommandParser.Parse(code, context);
            code = codeParseResult.RemainingCode;

            // Parse and execute registration commands from code
            if (codeParseResult.Commands.Count > 0)
            {
                RegistrationCommandExecutor.Register(eval, codeParseResult.Commands);
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
        /// Format exception details focusing on LoaderException information
        /// </summary>
        private static string FormatExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Type: {ex.GetType().Name}");
            
            // Add FileName for FileNotFoundException
            if (ex is System.IO.FileNotFoundException fnf)
            {
                if (!string.IsNullOrEmpty(fnf.FileName))
                {
                    sb.AppendLine($"FileName: {fnf.FileName}");
                }
                if (!string.IsNullOrEmpty(fnf.FusionLog))
                {
                    sb.AppendLine($"FusionLog: {fnf.FusionLog}");
                }
            }
            
            // Add FileName for FileLoadException (often contains strong name errors)
            if (ex is System.IO.FileLoadException fle)
            {
                if (!string.IsNullOrEmpty(fle.FileName))
                {
                    sb.AppendLine($"FileName: {fle.FileName}");
                }
                if (!string.IsNullOrEmpty(fle.FusionLog))
                {
                    sb.AppendLine($"FusionLog: {fle.FusionLog}");
                }
            }
            
            // Add FileName for BadImageFormatException
            if (ex is BadImageFormatException bif)
            {
                if (!string.IsNullOrEmpty(bif.FileName))
                {
                    sb.AppendLine($"FileName: {bif.FileName}");
                }
            }
            
            // Check LoaderException details for ReflectionTypeLoadException
            if (ex is ReflectionTypeLoadException rtle)
            {
                if (rtle.LoaderExceptions != null && rtle.LoaderExceptions.Length > 0)
                {
                    sb.AppendLine("LoaderExceptions:");
                    for (int i = 0; i < rtle.LoaderExceptions.Length; i++)
                    {
                        var loaderEx = rtle.LoaderExceptions[i];
                        if (loaderEx != null)
                        {
                            sb.AppendLine($"  [{i}] {loaderEx.GetType().Name}: {loaderEx.Message}");
                            
                            // Add FileName for FileNotFoundException
                            if (loaderEx is System.IO.FileNotFoundException loaderFnf && !string.IsNullOrEmpty(loaderFnf.FileName))
                            {
                                sb.AppendLine($"      文件名: {loaderFnf.FileName}");
                                if (!string.IsNullOrEmpty(loaderFnf.FusionLog))
                                {
                                    sb.AppendLine($"      FusionLog: {loaderFnf.FusionLog}");
                                }
                            }
                            
                            // Add FileName for FileLoadException (strong name errors)
                            if (loaderEx is System.IO.FileLoadException loaderFle && !string.IsNullOrEmpty(loaderFle.FileName))
                            {
                                sb.AppendLine($"      文件名: {loaderFle.FileName}");
                                if (!string.IsNullOrEmpty(loaderFle.FusionLog))
                                {
                                    sb.AppendLine($"      FusionLog: {loaderFle.FusionLog}");
                                }
                            }
                            
                            // Add FileName for BadImageFormatException
                            if (loaderEx is BadImageFormatException loaderBif && !string.IsNullOrEmpty(loaderBif.FileName))
                            {
                                sb.AppendLine($"      文件名: {loaderBif.FileName}");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"  [{i}] (null)");
                        }
                    }
                }
            }
            
            // Add inner exception details
            if (ex.InnerException != null)
            {
                sb.AppendLine($"InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                if (ex.InnerException is System.IO.FileNotFoundException innerFnf && !string.IsNullOrEmpty(innerFnf.FileName))
                {
                    sb.AppendLine($"InnerException FileName: {innerFnf.FileName}");
                }
                if (ex.InnerException is System.IO.FileLoadException innerFle && !string.IsNullOrEmpty(innerFle.FileName))
                {
                    sb.AppendLine($"InnerException FileName: {innerFle.FileName}");
                }
            }
            
            return sb.ToString();
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
                var details = FormatExceptionDetails(ex);
                _log.Error($"Failed to execute expression. Code: {code}\n{details}", ex);
                
                // Show detailed error message to user
                var errorMessage = $"表达式执行失败: {ex.Message}\n\n详细信息:\n{details}\n\n代码: {code}";
                AppHelper.ShowWarning(errorMessage);
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

