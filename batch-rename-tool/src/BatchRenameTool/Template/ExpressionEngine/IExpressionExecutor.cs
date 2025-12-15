using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Evaluator;

namespace BatchRenameTool.Template.ExpressionEngine
{
    /// <summary>
    /// Interface for executing template expression AST nodes
    /// Converts AST nodes to ITemplateValue instances
    /// </summary>
    public interface IExpressionExecutor
    {
        /// <summary>
        /// Execute an AST node and return a template value
        /// </summary>
        ITemplateValue Execute(AstNode node, IEvaluationContext context);
    }
}

