using System;
using System.Collections.Generic;
using System.Text;

namespace BatchRenameTool.Template.Lexer
{
    /// <summary>
    /// Lexer for template string parsing
    /// </summary>
    public class TemplateLexer
    {
        private readonly string _input;
        private int _position;
        private readonly List<Token> _tokens = new List<Token>();

        public TemplateLexer(string input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _position = 0;
        }

        /// <summary>
        /// Tokenize the input string
        /// </summary>
        public List<Token> Tokenize()
        {
            _tokens.Clear();
            _position = 0;

            while (_position < _input.Length)
            {
                var token = ReadNextToken();
                if (token != null)
                {
                    _tokens.Add(token);
                }
            }

            _tokens.Add(new Token(TokenType.EOF, "", _position));
            return _tokens;
        }

        private Token? ReadNextToken()
        {
            if (_position >= _input.Length)
                return null;

            var ch = _input[_position];

            // Skip whitespace (but preserve it in text tokens)
            if (char.IsWhiteSpace(ch))
            {
                return ReadText();
            }

            // Check if we're inside braces (for determining if special chars should be tokens or text)
            bool insideBraces = IsInsideBraces();

            switch (ch)
            {
                case '{':
                    _position++;
                    return new Token(TokenType.LeftBrace, "{", _position - 1);

                case '}':
                    _position++;
                    return new Token(TokenType.RightBrace, "}", _position - 1);

                case ':':
                    // Colon is only a special token inside braces (for format specifier)
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.Colon, ":", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case '.':
                    // Dot is only a special token inside braces (for method calls)
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.Dot, ".", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case '[':
                    // Left bracket is only a special token inside braces (for slicing)
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.LeftBracket, "[", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case ']':
                    // Right bracket is only a special token inside braces
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.RightBracket, "]", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case ',':
                    // Comma is only a special token inside braces (for method parameters)
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.Comma, ",", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case '(':
                    // Left parenthesis is only a special token inside braces (for method calls)
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.LeftParen, "(", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case ')':
                    // Right parenthesis is only a special token inside braces
                    if (insideBraces)
                    {
                        _position++;
                        return new Token(TokenType.RightParen, ")", _position - 1);
                    }
                    // Outside braces, treat as text
                    return ReadText();

                case '"':
                case '\'':
                    // String literal (only inside braces)
                    if (insideBraces)
                    {
                        return ReadStringLiteral();
                    }
                    // Outside braces, treat as text
                    return ReadText();

                default:
                    // Read text or identifier
                    if (IsIdentifierStart(ch))
                    {
                        return ReadIdentifier();
                    }
                    else
                    {
                        return ReadText();
                    }
            }
        }

        /// <summary>
        /// Check if current position is inside braces (between { and })
        /// </summary>
        private bool IsInsideBraces()
        {
            int braceDepth = 0;
            for (int i = 0; i < _position; i++)
            {
                if (_input[i] == '{')
                    braceDepth++;
                else if (_input[i] == '}')
                    braceDepth--;
            }
            return braceDepth > 0;
        }

        private Token ReadText()
        {
            var start = _position;
            var sb = new StringBuilder();

            while (_position < _input.Length)
            {
                var ch = _input[_position];

                // Stop at special characters
                if (ch == '{' || ch == '}')
                {
                    break;
                }

                // Stop at other operators if we're inside braces
                if (IsSpecialChar(ch))
                {
                    // Check if we're inside braces (look back for '{')
                    bool insideBraces = false;
                    for (int i = _position - 1; i >= 0; i--)
                    {
                        if (_input[i] == '}')
                            break;
                        if (_input[i] == '{')
                        {
                            insideBraces = true;
                            break;
                        }
                    }

                    if (insideBraces)
                    {
                        break;
                    }
                }

                sb.Append(ch);
                _position++;
            }

            var text = sb.ToString();
            return text.Length > 0
                ? new Token(TokenType.Text, text, start)
                : null;
        }

        private Token ReadStringLiteral()
        {
            var start = _position;
            var quoteChar = _input[_position]; // ' or "
            _position++; // Skip opening quote

            var sb = new StringBuilder();

            while (_position < _input.Length)
            {
                var ch = _input[_position];

                if (ch == quoteChar)
                {
                    // Found closing quote
                    _position++;
                    return new Token(TokenType.StringLiteral, sb.ToString(), start);
                }
                else if (ch == '\\' && _position + 1 < _input.Length)
                {
                    // Handle escape sequences
                    _position++;
                    var escapedChar = _input[_position];
                    switch (escapedChar)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        case '\'': sb.Append('\''); break;
                        default: sb.Append(escapedChar); break; // Unknown escape, include as-is
                    }
                    _position++;
                }
                else
                {
                    sb.Append(ch);
                    _position++;
                }
            }

            // Unterminated string literal
            throw new Exception($"Unterminated string literal starting at position {start}");
        }

        private Token ReadIdentifier()
        {
            var start = _position;
            var sb = new StringBuilder();

            while (_position < _input.Length)
            {
                var ch = _input[_position];

                if (IsIdentifierChar(ch))
                {
                    sb.Append(ch);
                    _position++;
                }
                else
                {
                    break;
                }
            }

            return new Token(TokenType.Identifier, sb.ToString(), start);
        }

        private static bool IsIdentifierStart(char ch)
        {
            return char.IsLetter(ch) || ch == '_';
        }

        private static bool IsIdentifierChar(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        private static bool IsSpecialChar(char ch)
        {
            return ch == ':' || ch == '.' || ch == '[' || ch == ']' || ch == ',' || ch == '(' || ch == ')';
        }
    }
}
