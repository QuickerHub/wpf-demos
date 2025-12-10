using System;
using System.Text;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Utils;

namespace BatchRenameTool.Template.Evaluator
{
    /// <summary>
    /// Context for template evaluation
    /// </summary>
    public class EvaluationContext
    {
        public string Name { get; set; } = "";           // File name without extension
        public string Ext { get; set; } = "";             // Extension without dot
        public string FullName { get; set; } = "";       // Full file name
        public int Index { get; set; } = 0;              // Index for {i} variable
    }

    /// <summary>
    /// Evaluator for template AST
    /// </summary>
    public class TemplateEvaluator
    {
        /// <summary>
        /// Evaluate template node with context
        /// </summary>
        public string Evaluate(TemplateNode node, EvaluationContext context)
        {
            var sb = new StringBuilder();

            foreach (var astNode in node.Nodes)
            {
                sb.Append(EvaluateNode(astNode, context));
            }

            return sb.ToString();
        }

        private string EvaluateNode(AstNode node, EvaluationContext context)
        {
            return node switch
            {
                TextNode textNode => textNode.Text,
                VariableNode varNode => EvaluateVariable(varNode, context),
                FormatNode formatNode => EvaluateFormat(formatNode, context),
                MethodNode methodNode => EvaluateMethod(methodNode, context),
                SliceNode sliceNode => EvaluateSlice(sliceNode, context),
                _ => $"[未支持的节点类型: {node.GetType().Name}]"
            };
        }

        private string EvaluateVariable(VariableNode node, EvaluationContext context)
        {
            return node.VariableName.ToLower() switch
            {
                "name" => context.Name,
                "ext" => context.Ext,
                "fullname" => context.FullName,
                "i" => context.Index.ToString(),
                _ => $"{{{node.VariableName}}}"
            };
        }

        private string EvaluateFormat(FormatNode node, EvaluationContext context)
        {
            // Inner node should be a VariableNode (for now, only {i:format} is supported)
            if (node.InnerNode is VariableNode varNode && varNode.VariableName.ToLower() == "i")
            {
                return FormatIndex(context.Index, node.FormatString);
            }

            // For other variables, format is not supported yet
            return EvaluateNode(node.InnerNode, context);
        }

        private string FormatIndex(int index, string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                return index.ToString();
            }

            // Check if it's a Chinese number format
            if (ChineseNumberConverter.IsChineseNumber(formatString[0]))
            {
                return FormatChineseIndex(index, formatString);
            }

            // Numeric format
            return FormatNumericIndex(index, formatString);
        }

        private string FormatNumericIndex(int index, string formatString)
        {
            // Parse format string to determine start value and padding
            // Examples: "00" -> start from 00, "01" -> start from 01, "1" -> start from 1

            int startValue = 0;
            int padding = 0;

            // Try to parse the first character as a digit
            if (formatString.Length > 0 && char.IsDigit(formatString[0]))
            {
                // Determine start value from first character
                startValue = formatString[0] - '0';

                // Count padding zeros (length of format string)
                padding = formatString.Length;

                // Special handling:
                // - "00", "000" -> start from 0, pad with zeros
                // - "01", "001" -> start from 1, pad with zeros
                // - "1" -> start from 1, no padding
                if (startValue == 0 && formatString.Length > 1)
                {
                    // Check if second character is non-zero (like "01", "02")
                    if (formatString[1] != '0')
                    {
                        // Format like "01", "02" - start from 1
                        startValue = 1;
                    }
                    // Otherwise "00", "000" - start from 0
                }
            }

            int value = index + startValue;
            
            // Apply padding if format string has multiple digits
            if (padding > 0)
            {
                return value.ToString($"D{padding}");
            }
            
            return value.ToString();
        }

        private string FormatChineseIndex(int index, string formatString)
        {
            // Check if upper case Chinese numbers are used
            bool useUpper = ChineseNumberConverter.IsUpperChineseNumber(formatString[0]);

            // Determine start value from first character
            int startValue = 0;

            // Map Chinese number to start value
            var firstChar = formatString[0].ToString();
            var lowerDigits = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            var upperDigits = new[] { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };

            var digits = useUpper ? upperDigits : lowerDigits;
            for (int i = 0; i < digits.Length; i++)
            {
                if (digits[i] == firstChar)
                {
                    startValue = i;
                    break;
                }
            }

            // Special case: "十" or "拾" means start from 10
            if (formatString[0] == '十' || formatString[0] == '拾')
            {
                startValue = 10;
            }

            int value = index + startValue;
            return ChineseNumberConverter.ToChinese(value, useUpper);
        }

        private string EvaluateMethod(MethodNode node, EvaluationContext context)
        {
            // Evaluate target first
            var targetValue = EvaluateNode(node.Target, context);
            
            // For now, return error message - method implementation will be added later
            return $"[方法调用未实现: {node.MethodName}]";
        }

        private string EvaluateSlice(SliceNode node, EvaluationContext context)
        {
            // Evaluate target first
            var targetValue = EvaluateNode(node.Target, context);
            
            // For now, return error message - slice implementation will be added later
            return $"[切片未实现]";
        }
    }
}
