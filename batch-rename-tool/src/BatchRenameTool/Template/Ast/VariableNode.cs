namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Variable node: {name}, {ext}, {fullname}, {i}
    /// </summary>
    public class VariableNode : AstNode
    {
        public string VariableName { get; }

        public VariableNode(string variableName)
        {
            VariableName = variableName;
        }
    }
}
