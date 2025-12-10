using System.Collections.Generic;

namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Method call node: {name.replace(old,new)}, {name.upper()}
    /// </summary>
    public class MethodNode : AstNode
    {
        public AstNode Target { get; }
        public string MethodName { get; }
        public List<AstNode> Arguments { get; }

        public MethodNode(AstNode target, string methodName, List<AstNode>? arguments = null)
        {
            Target = target;
            MethodName = methodName;
            Arguments = arguments ?? new List<AstNode>();
        }
    }
}
