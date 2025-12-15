using System.Text;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Evaluator;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Executes a complete template node (root node containing multiple child nodes)
    /// Combines all child node results into a single string
    /// </summary>
    public class TemplateNodeExecutor
    {
        private readonly IExpressionExecutor _executor;

        public TemplateNodeExecutor(IExpressionExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Execute a template node and return the final string result
        /// </summary>
        public string Execute(TemplateNode node, IEvaluationContext context)
        {
            if (node == null || node.Nodes == null || node.Nodes.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();

            foreach (var childNode in node.Nodes)
            {
                var value = _executor.Execute(childNode, context);
                sb.Append(value.ToString());
            }

            return sb.ToString();
        }
    }
}

