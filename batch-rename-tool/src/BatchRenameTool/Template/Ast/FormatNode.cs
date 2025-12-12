namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Format node: {i:00}, {i:一}, {i:2*i+1:000}
    /// </summary>
    public class FormatNode : AstNode
    {
        public AstNode InnerNode { get; }
        public string? ExpressionString { get; } // Optional expression like "2*i+1"
        public string FormatString { get; }

        public FormatNode(AstNode innerNode, string formatString)
        {
            InnerNode = innerNode;
            FormatString = formatString;
            ExpressionString = null;
        }

        public FormatNode(AstNode innerNode, string? expressionString, string formatString)
        {
            InnerNode = innerNode;
            ExpressionString = expressionString;
            FormatString = formatString;
        }
    }
}
