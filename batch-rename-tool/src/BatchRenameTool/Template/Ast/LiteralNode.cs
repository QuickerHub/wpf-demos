namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Literal value node (for method arguments, slice indices, etc.)
    /// </summary>
    public class LiteralNode : AstNode
    {
        public object Value { get; }

        public LiteralNode(object value)
        {
            Value = value;
        }
    }
}
