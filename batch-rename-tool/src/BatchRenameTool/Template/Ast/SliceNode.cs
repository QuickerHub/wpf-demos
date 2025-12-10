namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Slice node: {name[1:3]}, {name[:3]}, {name[1:]}
    /// This is syntax sugar for sub() method calls
    /// </summary>
    public class SliceNode : AstNode
    {
        public AstNode Target { get; }
        public AstNode? Start { get; }  // null means from start
        public AstNode? End { get; }    // null means to end

        public SliceNode(AstNode target, AstNode? start = null, AstNode? end = null)
        {
            Target = target;
            Start = start;
            End = end;
        }
    }
}
