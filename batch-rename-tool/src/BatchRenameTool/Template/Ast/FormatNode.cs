namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Format node: {i:00}, {i:一}
    /// </summary>
    public class FormatNode : AstNode
    {
        public AstNode InnerNode { get; }
        public string FormatString { get; }

        public FormatNode(AstNode innerNode, string formatString)
        {
            InnerNode = innerNode;
            FormatString = formatString;
        }
    }
}
