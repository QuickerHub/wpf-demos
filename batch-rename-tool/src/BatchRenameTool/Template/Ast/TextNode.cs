namespace BatchRenameTool.Template.Ast
{
    /// <summary>
    /// Text node for plain text content
    /// </summary>
    public class TextNode : AstNode
    {
        public string Text { get; }

        public TextNode(string text)
        {
            Text = text;
        }
    }
}
