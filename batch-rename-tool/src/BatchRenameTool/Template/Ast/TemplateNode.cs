using System.Collections.Generic;

namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Root template node containing a list of nodes
    /// </summary>
    public class TemplateNode : AstNode
    {
        public List<AstNode> Nodes { get; }

        public TemplateNode(List<AstNode> nodes)
        {
            Nodes = nodes;
        }
    }
}
