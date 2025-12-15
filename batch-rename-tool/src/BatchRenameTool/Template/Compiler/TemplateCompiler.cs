using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.ExpressionEngine;

namespace BatchRenameTool.Template.Compiler
{
    /// <summary>
    /// Compiler that converts template AST nodes into compiled functions for better performance
    /// </summary>
    public class TemplateCompiler
    {
        /// <summary>
        /// Compile a template node into a function
        /// </summary>
        public Func<IEvaluationContext, string> Compile(TemplateNode node)
        {
            if (node == null || node.Nodes == null || node.Nodes.Count == 0)
            {
                return ctx => "";
            }

            // Compile each child node into a function
            var compiledNodes = node.Nodes.Select(CompileNode).ToList();

            // If only one node, return it directly
            if (compiledNodes.Count == 1)
            {
                return compiledNodes[0];
            }

            // Combine multiple nodes using StringBuilder
            return ctx =>
            {
                var sb = new StringBuilder();
                foreach (var func in compiledNodes)
                {
                    sb.Append(func(ctx));
                }
                return sb.ToString();
            };
        }

        /// <summary>
        /// Compile a single AST node into a function
        /// </summary>
        private Func<IEvaluationContext, string> CompileNode(AstNode node)
        {
            return node switch
            {
                TextNode textNode => CompileTextNode(textNode),
                VariableNode varNode => CompileVariableNode(varNode),
                FormatNode formatNode => CompileFormatNode(formatNode),
                MethodNode methodNode => CompileMethodNode(methodNode),
                SliceNode sliceNode => CompileSliceNode(sliceNode),
                _ => ctx => $"[Unknown node: {node.GetType().Name}]"
            };
        }

        /// <summary>
        /// Compile TextNode - return constant string
        /// </summary>
        private Func<IEvaluationContext, string> CompileTextNode(TextNode node)
        {
            var text = node.Text; // Capture text at compile time
            return ctx => text;
        }

        /// <summary>
        /// Compile VariableNode - direct property access
        /// </summary>
        private Func<IEvaluationContext, string> CompileVariableNode(VariableNode node)
        {
            var varName = node.VariableName.ToLower();
            return varName switch
            {
                "name" => ctx => ctx.Name,
                "ext" => ctx => ctx.Ext,
                "fullname" => ctx => ctx.FullName,
                "dirname" => ctx => ctx.DirName,
                "i" => ctx => ctx.Index.ToString(),
                "iv" => ctx => (ctx.TotalCount - 1 - ctx.Index).ToString(),
                "today" => ctx => new DateValue(ctx.Today).ToString(),
                "now" => ctx => new DateValue(ctx.Now).ToString(),
                "image" => ctx => new ImageValue(ctx.Image).ToString(),
                "file" => ctx => new FileValue(ctx).ToString(),
                "size" => ctx => new SizeValue(ctx.Size).ToString(),
                _ => ctx => $"{{{node.VariableName}}}"
            };
        }

        /// <summary>
        /// Compile FormatNode - compile inner node and apply formatting
        /// </summary>
        private Func<IEvaluationContext, string> CompileFormatNode(FormatNode node)
        {
            var formatString = node.FormatString;

            // Handle expression format: {2i+1:00} or {i2+1} (without format)
            if (!string.IsNullOrEmpty(node.ExpressionString))
            {
                var expression = node.ExpressionString;
                var compiledExpr = CompileExpression(expression);
                
                // If format string is empty, just return the expression result as string
                if (string.IsNullOrEmpty(formatString))
                {
                    return ctx => compiledExpr(ctx.Index).ToString();
                }
                
                // Otherwise, format the expression result using IndexValue
                return ctx =>
                {
                    var indexValue = new IndexValue(compiledExpr(ctx.Index), ctx.TotalCount);
                    return indexValue.EvaluateExpression("", formatString).ToString();
                };
            }

            // Handle variable format: {i:001}, {today:yyyyMMdd}, etc.
            if (node.InnerNode is VariableNode varNode)
            {
                var varName = varNode.VariableName.ToLower();
                
                // Handle index variables with format using IndexValue
                if (varName == "i")
                {
                    return ctx =>
                    {
                        var indexValue = new IndexValue(ctx.Index, ctx.TotalCount);
                        return indexValue.ToString(formatString);
                    };
                }
                
                if (varName == "iv")
                {
                    return ctx =>
                    {
                        int reverseIndex = ctx.TotalCount > 0 ? ctx.TotalCount - 1 - ctx.Index : 0;
                        var indexValue = new IndexValue(reverseIndex, ctx.TotalCount);
                        return indexValue.ToString(formatString);
                    };
                }
                
                // Handle date variables using DateValue
                if (varName == "today")
                {
                    return ctx => new DateValue(ctx.Today).ToString(formatString);
                }
                
                if (varName == "now")
                {
                    return ctx => new DateValue(ctx.Now).ToString(formatString);
                }
                
                // Handle image variable using ImageValue
                if (varName == "image")
                {
                    return ctx => new ImageValue(ctx.Image).ToString(formatString);
                }
                
                // Handle file variable using FileValue
                if (varName == "file")
                {
                    return ctx => new FileValue(ctx).ToString(formatString);
                }
                
                // Handle size variable using SizeValue
                if (varName == "size")
                {
                    return ctx => new SizeValue(ctx.Size).ToString(formatString);
                }
            }

            // For other cases, compile inner node and apply format (if supported)
            var innerFunc = CompileNode(node.InnerNode);
            return innerFunc; // Format not supported for other node types
        }

        /// <summary>
        /// Compile MethodNode - compile target and arguments, then apply method
        /// </summary>
        private Func<IEvaluationContext, string> CompileMethodNode(MethodNode node)
        {
            var targetFunc = CompileNode(node.Target);
            var methodName = node.MethodName.ToLower();
            
            // Compile arguments
            var argFuncs = new List<Func<IEvaluationContext, object>>();
            foreach (var arg in node.Arguments)
            {
                if (arg is LiteralNode literalNode)
                {
                    // Capture literal value at compile time
                    var value = literalNode.Value;
                    argFuncs.Add(ctx => value);
                }
                else
                {
                    // Compile complex argument expression
                    var argFunc = CompileNode(arg);
                    argFuncs.Add(ctx => argFunc(ctx));
                }
            }

            return methodName switch
            {
                "upper" => ctx => targetFunc(ctx).ToUpper(),
                "lower" => ctx => targetFunc(ctx).ToLower(),
                "trim" => ctx => targetFunc(ctx).Trim(),
                "replace" => CompileReplaceMethod(targetFunc, argFuncs),
                "sub" => CompileSubMethod(targetFunc, argFuncs),
                "slice" => CompileSliceMethod(targetFunc, argFuncs),
                "padleft" => CompilePadLeftMethod(targetFunc, argFuncs),
                "padright" => CompilePadRightMethod(targetFunc, argFuncs),
                _ => ctx => $"[Unknown method: {node.MethodName}]"
            };
        }

        /// <summary>
        /// Compile SliceNode - compile target and indices, then apply slice
        /// Note: SliceNode is kept for backward compatibility, but new code uses MethodNode with "slice" method
        /// </summary>
        private Func<IEvaluationContext, string> CompileSliceNode(SliceNode node)
        {
            var targetFunc = CompileNode(node.Target);
            
            // Convert SliceNode to argument functions for CompileSliceMethod
            var argFuncs = new List<Func<IEvaluationContext, object>>();
            
            if (node.Start != null)
            {
                if (node.Start is LiteralNode startLiteral && startLiteral.Value is int startInt)
                {
                    // Capture literal at compile time
                    argFuncs.Add(ctx => startInt);
                }
                else
                {
                    var startNodeFunc = CompileNode(node.Start);
                    argFuncs.Add(ctx => startNodeFunc(ctx));
                }
            }
            
            if (node.End != null)
            {
                if (node.End is LiteralNode endLiteral && endLiteral.Value is int endInt)
                {
                    // Capture literal at compile time
                    argFuncs.Add(ctx => endInt);
                }
                else
                {
                    var endNodeFunc = CompileNode(node.End);
                    argFuncs.Add(ctx => endNodeFunc(ctx));
                }
            }
            
            // Reuse CompileSliceMethod logic
            return CompileSliceMethod(targetFunc, argFuncs);
        }

        #region Helper Methods for Method Compilation

        private Func<IEvaluationContext, string> CompileReplaceMethod(
            Func<IEvaluationContext, string> targetFunc,
            List<Func<IEvaluationContext, object>> argFuncs)
        {
            if (argFuncs.Count < 2)
            {
                return targetFunc; // Not enough arguments
            }
            
            return ctx =>
            {
                var target = targetFunc(ctx);
                var oldValue = argFuncs[0](ctx)?.ToString() ?? "";
                var newValue = argFuncs[1](ctx)?.ToString() ?? "";
                return target.Replace(oldValue, newValue);
            };
        }

        private Func<IEvaluationContext, string> CompileSubMethod(
            Func<IEvaluationContext, string> targetFunc,
            List<Func<IEvaluationContext, object>> argFuncs)
        {
            // sub() is similar to slice(), reuse slice logic
            return CompileSliceMethod(targetFunc, argFuncs);
        }

        private Func<IEvaluationContext, string> CompileSliceMethod(
            Func<IEvaluationContext, string> targetFunc,
            List<Func<IEvaluationContext, object>> argFuncs)
        {
            // slice() - no arguments, return full string
            if (argFuncs.Count == 0)
            {
                return targetFunc;
            }
            
            // slice(start) - from start to end
            if (argFuncs.Count == 1)
            {
                return ctx =>
                {
                    var target = targetFunc(ctx);
                    int start = ParseIndex(argFuncs[0](ctx), target.Length);
                    return target.Substring(start);
                };
            }
            
            // slice(start, end) - from start to end
            return ctx =>
            {
                var target = targetFunc(ctx);
                int start = ParseIndex(argFuncs[0](ctx), target.Length);
                int end = ParseIndex(argFuncs[1](ctx), target.Length, defaultValue: target.Length);
                
                if (start >= end) return "";
                return target.Substring(start, end - start);
            };
        }

        /// <summary>
        /// Parse an index value from an object, handling negative indices and bounds checking
        /// </summary>
        private int ParseIndex(object? indexObj, int length, int defaultValue = 0)
        {
            int index = indexObj is int indexInt ? indexInt :
                       (int.TryParse(indexObj?.ToString(), out int parsed) ? parsed : defaultValue);
            
            // Handle negative indices
            if (index < 0) index = length + index;
            
            // Clamp to valid range
            return Math.Max(0, Math.Min(index, length));
        }

        private Func<IEvaluationContext, string> CompilePadLeftMethod(
            Func<IEvaluationContext, string> targetFunc,
            List<Func<IEvaluationContext, object>> argFuncs)
        {
            return CompilePadMethod(targetFunc, argFuncs, isLeft: true);
        }

        private Func<IEvaluationContext, string> CompilePadRightMethod(
            Func<IEvaluationContext, string> targetFunc,
            List<Func<IEvaluationContext, object>> argFuncs)
        {
            return CompilePadMethod(targetFunc, argFuncs, isLeft: false);
        }

        private Func<IEvaluationContext, string> CompilePadMethod(
            Func<IEvaluationContext, string> targetFunc,
            List<Func<IEvaluationContext, object>> argFuncs,
            bool isLeft)
        {
            if (argFuncs.Count == 0)
            {
                return targetFunc;
            }
            
            return ctx =>
            {
                var target = targetFunc(ctx);
                var widthObj = argFuncs[0](ctx);
                int totalWidth = widthObj is int widthInt ? widthInt :
                                (int.TryParse(widthObj?.ToString(), out int parsed) ? parsed : target.Length);
                
                char paddingChar = ' ';
                if (argFuncs.Count >= 2)
                {
                    var paddingStr = argFuncs[1](ctx)?.ToString() ?? " ";
                    if (paddingStr.Length > 0) paddingChar = paddingStr[0];
                }
                
                return isLeft ? target.PadLeft(totalWidth, paddingChar) : target.PadRight(totalWidth, paddingChar);
            };
        }

        #endregion

        #region Formatting Helpers
        // All formatting methods have been moved to value type classes:
        // - IndexValue: handles index formatting (numeric and Chinese)
        // - DateValue: handles date/time formatting
        // - ImageValue: handles image dimension formatting
        // - SizeValue: handles file size formatting
        // - FileValue: handles file information formatting
        #endregion

        #region Expression Compilation

        /// <summary>
        /// Compile expression like "2*i+1" into a function that takes index and returns result
        /// Uses the same logic as TemplateEvaluator.EvaluateExpression for consistency
        /// </summary>
        private Func<int, int> CompileExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return index => index;
            }

            // Capture expression at compile time (don't normalize yet)
            var expr = expression;

            // Return function that evaluates expression at runtime using same logic as evaluator
            return index =>
            {
                try
                {
                    // Use the same logic as TemplateEvaluator.EvaluateExpression
                    // Remove whitespace
                    var normalized = expr.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
                    
                    if (string.IsNullOrEmpty(normalized))
                    {
                        return index;
                    }

                    // Normalize expression: replace implicit multiplication (e.g., "2i" -> "2*i")
                    normalized = Regex.Replace(normalized, @"(\d+)([iI])", "$1*$2", RegexOptions.IgnoreCase);
                    normalized = Regex.Replace(normalized, @"([iI])(\d+)", "$1*$2", RegexOptions.IgnoreCase);
                    
                    // Replace 'i' with the actual index value
                    normalized = Regex.Replace(normalized, @"\b[iI]\b", index.ToString(), RegexOptions.IgnoreCase);
                    
                    // Validate expression contains only numbers, operators, and parentheses
                    if (!Regex.IsMatch(normalized, @"^[0-9+\-*/().\s]+$"))
                    {
                        return index; // Invalid expression
                    }

                    // Use DataTable to evaluate the expression safely
                    var dataTable = new DataTable();
                    var result = dataTable.Compute(normalized, null);
                    
                    // Convert to integer
                    return Convert.ToInt32(Math.Round(Convert.ToDouble(result)));
                }
                catch
                {
                    return index;
                }
            };
        }

        #endregion

        #region Slice Helper
        // SliceString method removed - slice logic is now handled directly in CompileSliceMethod
        #endregion
    }
}

