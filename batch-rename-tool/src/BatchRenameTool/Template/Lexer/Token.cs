namespace BatchRenameTool.Template.Lexer
{
    /// <summary>
    /// Token types for lexer
    /// </summary>
    public enum TokenType
    {
        Text,           // Plain text
        LeftBrace,      // {
        RightBrace,     // }
        Colon,          // :
        Dot,            // .
        LeftBracket,    // [
        RightBracket,   // ]
        LeftParen,      // (
        RightParen,     // )
        Comma,          // ,
        Identifier,     // Variable name, method name, etc.
        EOF             // End of file
    }

    /// <summary>
    /// Token representation
    /// </summary>
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Position { get; }

        public Token(TokenType type, string value, int position)
        {
            Type = type;
            Value = value;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Type}({Value})";
        }
    }
}
