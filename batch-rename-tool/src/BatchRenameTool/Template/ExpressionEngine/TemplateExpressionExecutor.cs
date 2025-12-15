using System;
using System.Collections.Generic;
using System.Linq;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Evaluator;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Executes template expression AST nodes and converts them to ITemplateValue instances
    /// Supports method chaining and nested expressions
    /// </summary>
    public class TemplateExpressionExecutor : IExpressionExecutor
    {
        /// <summary>
        /// Execute an AST node and return a template value
        /// </summary>
        public ITemplateValue Execute(AstNode node, IEvaluationContext context)
        {
            return node switch
            {
                TextNode textNode => new StringValue(textNode.Text),
                VariableNode varNode => ExecuteVariable(varNode, context),
                FormatNode formatNode => ExecuteFormat(formatNode, context),
                MethodNode methodNode => ExecuteMethod(methodNode, context),
                SliceNode sliceNode => ExecuteSlice(sliceNode, context),
                LiteralNode literalNode => ExecuteLiteral(literalNode),
                _ => throw new NotSupportedException($"Node type {node.GetType().Name} is not supported")
            };
        }

        private ITemplateValue ExecuteVariable(VariableNode node, IEvaluationContext context)
        {
            var varName = node.VariableName.ToLower();
            return varName switch
            {
                "name" => new StringValue(context.Name),
                "ext" => new StringValue(context.Ext),
                "fullname" => new StringValue(context.FullName),
                "dirname" => new StringValue(context.DirName),
                "i" => new IndexValue(context.Index, context.TotalCount),
                "iv" => new IndexValue(context.TotalCount - 1 - context.Index, context.TotalCount),
                "today" => new DateValue(context.Today),
                "now" => new DateValue(context.Now),
                "image" => new ImageValue(context.Image),
                "file" => new FileValue(context),
                "size" => new SizeValue(context.Size),
                _ => new StringValue($"{{{node.VariableName}}}")
            };
        }

        private ITemplateValue ExecuteFormat(FormatNode node, IEvaluationContext context)
        {
            var formatString = node.FormatString;

            // Handle expression format: {2i+1:00}
            if (!string.IsNullOrEmpty(node.ExpressionString))
            {
                var expression = node.ExpressionString;
                var indexValue = new IndexValue(context.Index, context.TotalCount);
                return indexValue.EvaluateExpression(expression, formatString);
            }

            // Handle variable format: {i:001}, {today:yyyyMMdd}, etc.
            var innerValue = Execute(node.InnerNode, context);
            
            // Return formatted string as StringValue
            return new StringValue(innerValue.ToString(formatString));
        }

        private ITemplateValue ExecuteMethod(MethodNode node, IEvaluationContext context)
        {
            // Execute target expression to get the value
            var targetValue = Execute(node.Target, context);

            // Execute all argument expressions
            var arguments = node.Arguments.Select(arg => Execute(arg, context)).ToList();

            // Invoke method on the target value
            return targetValue.InvokeMethod(node.MethodName, arguments);
        }

        private ITemplateValue ExecuteSlice(SliceNode node, IEvaluationContext context)
        {
            // Execute target expression
            var targetValue = Execute(node.Target, context);

            // Convert to string for slicing
            var targetString = targetValue.ToString();

            // Execute start and end indices
            int? startIndex = null;
            int? endIndex = null;

            if (node.Start != null)
            {
                var startValue = Execute(node.Start, context);
                var startObj = startValue.GetValue();
                if (startObj is int startInt)
                {
                    startIndex = startInt;
                }
                else if (int.TryParse(startObj?.ToString(), out int parsed))
                {
                    startIndex = parsed;
                }
            }

            if (node.End != null)
            {
                var endValue = Execute(node.End, context);
                var endObj = endValue.GetValue();
                if (endObj is int endInt)
                {
                    endIndex = endInt;
                }
                else if (int.TryParse(endObj?.ToString(), out int parsed))
                {
                    endIndex = parsed;
                }
            }

            // Handle slice indices (Python-style)
            int start = startIndex ?? 0;
            int end = endIndex ?? targetString.Length;

            // Handle negative indices
            if (start < 0) start = targetString.Length + start;
            if (end < 0) end = targetString.Length + end;

            // Clamp to valid range
            start = Math.Max(0, Math.Min(start, targetString.Length));
            end = Math.Max(0, Math.Min(end, targetString.Length));

            // Extract substring
            if (start >= end)
            {
                return new StringValue("");
            }

            return new StringValue(targetString.Substring(start, end - start));
        }

        private ITemplateValue ExecuteLiteral(LiteralNode node)
        {
            var value = node.Value;
            return value switch
            {
                string str => new StringValue(str),
                int i => new NumberValue(i),
                long l => new NumberValue((int)l),
                double d => new NumberValue((int)d),
                _ => new StringValue(value?.ToString() ?? "")
            };
        }

    }
}

