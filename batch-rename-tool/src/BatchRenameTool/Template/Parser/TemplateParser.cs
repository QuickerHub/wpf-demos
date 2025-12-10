using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Lexer;

namespace BatchRenameTool.Template.Parser
{
    /// <summary>
    /// Parser for template strings
    /// </summary>
    public class TemplateParser
    {
        private readonly Dictionary<string, MethodInfo> _methodMap = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, string> _methodAliasMap = new Dictionary<string, string>();
        private List<Token> _tokens = new List<Token>();
        private int _currentIndex = 0;

        /// <summary>
        /// Initialize parser with extension types
        /// </summary>
        public TemplateParser(IEnumerable<Type> extensionTypes)
        {
            BuildAliasMap(extensionTypes);
        }

        /// <summary>
        /// Build alias map from extension types
        /// </summary>
        private void BuildAliasMap(IEnumerable<Type> extensionTypes)
        {
            foreach (var extensionType in extensionTypes)
            {
                // Scan all public static methods
                var methods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach (var method in methods)
                {
                    // Check for MethodAlias attribute
                    var aliasAttr = method.GetCustomAttribute<MethodAliasAttribute>();
                    if (aliasAttr != null)
                    {
                        var methodName = method.Name.ToLower(); // Convert to lowercase for storage

                        // Store method info (for later invocation)
                        if (!_methodMap.ContainsKey(methodName))
                        {
                            _methodMap[methodName] = method;
                        }

                        // Map each alias to the method name (aliases also converted to lowercase)
                        // Support multiple aliases, each maps to the same method name
                        foreach (var alias in aliasAttr.Aliases)
                        {
                            var aliasLower = alias.ToLower();
                            if (!_methodAliasMap.ContainsKey(aliasLower))
                            {
                                _methodAliasMap[aliasLower] = methodName;
                            }
                            // If alias conflicts, use the first definition
                        }

                        // Also add the method name itself to the map (for case-insensitive support)
                        if (!_methodAliasMap.ContainsKey(methodName))
                        {
                            _methodAliasMap[methodName] = methodName;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parse template string into AST
        /// </summary>
        public TemplateNode Parse(string template)
        {
            var lexer = new TemplateLexer(template);
            _tokens = lexer.Tokenize();
            _currentIndex = 0;

            var nodes = new List<AstNode>();

            while (!IsAtEnd())
            {
                var node = ParseNode();
                if (node != null)
                {
                    nodes.Add(node);
                }
            }

            return new TemplateNode(nodes);
        }

        private AstNode? ParseNode()
        {
            var token = Peek();

            if (token.Type == TokenType.Text)
            {
                return ParseText();
            }
            else if (token.Type == TokenType.LeftBrace)
            {
                return ParseExpression();
            }
            else if (token.Type == TokenType.EOF)
            {
                return null; // End of input
            }
            else if (token.Type == TokenType.Identifier)
            {
                // Identifier outside of braces should be treated as text
                // This handles cases like "aaabbb" or "{name}prefix{ext}"
                var tokenValue = Advance().Value;
                return new TextNode(tokenValue);
            }
            else
            {
                // For tokens like Dot, Colon, etc. outside of expressions, treat as text
                // This handles cases like "{name}.{ext}" where '.' is between expressions
                var tokenValue = Advance().Value;
                return new TextNode(tokenValue);
            }
        }

        private AstNode ParseText()
        {
            var token = Advance();
            return new TextNode(token.Value);
        }

        private AstNode ParseExpression()
        {
            // Consume '{'
            Advance();

            // Parse variable first (format specifier only applies to variables, not method calls)
            AstNode node = ParseVariable();

            // Check for format specifier (only for 'i' variable)
            if (Peek().Type == TokenType.Colon && node is VariableNode varNode && varNode.VariableName.ToLower() == "i")
            {
                Advance(); // Consume ':'
                
                // Read format string until '}' or method call/slice
                var formatString = ReadFormatString();
                
                node = new FormatNode(node, formatString);
            }
            else
            {
                // Parse method calls and slices (chain them)
                node = ParseMethodCallsAndSlices(node);
            }

            // Consume '}'
            if (Peek().Type != TokenType.RightBrace)
            {
                throw new ParseException($"Expected '}}' at position {Peek().Position}");
            }
            Advance();

            return node;
        }

        private AstNode ParseMethodCallsAndSlices(AstNode node)
        {
            // Parse method calls and slices (chain them)
            while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
            {
                var token = Peek();
                
                if (token.Type == TokenType.Dot)
                {
                    // Method call: .methodName() or .methodName
                    node = ParseMethodCall(node);
                }
                else if (token.Type == TokenType.LeftBracket)
                {
                    // Slice: [start:end] or [start:] or [:end] or [:]
                    node = ParseSlice(node);
                }
                else
                {
                    // No more method calls or slices
                    break;
                }
            }
            
            return node;
        }

        private string ReadFormatString()
        {
            var sb = new System.Text.StringBuilder();
            
            while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
            {
                var token = Advance();
                sb.Append(token.Value);
            }

            return sb.ToString();
        }


        private AstNode ParseVariable()
        {
            var token = Peek();

            if (token.Type == TokenType.Identifier)
            {
                var varName = Advance().Value;
                return new VariableNode(varName);
            }

            throw new ParseException($"Expected identifier, got {token.Type} at position {token.Position}");
        }

        private AstNode ParseMethodCall(AstNode target)
        {
            // Consume '.'
            Advance();
            
            // Parse method name
            var token = Peek();
            if (token.Type != TokenType.Identifier)
            {
                throw new ParseException($"Expected method name after '.', got {token.Type} at position {token.Position}");
            }
            
            var methodName = Advance().Value;
            
            // Check for parentheses (optional for parameterless methods)
            var arguments = new List<AstNode>();
            
            if (Peek().Type == TokenType.LeftParen)
            {
                Advance(); // Consume '('
                
                // Parse arguments
                while (!IsAtEnd() && Peek().Type != TokenType.RightParen && Peek().Type != TokenType.RightBrace)
                {
                    var nextToken = Peek();
                    
                    if (nextToken.Type == TokenType.Identifier)
                    {
                        var argValue = Advance().Value;
                        // Try to parse as number, otherwise treat as string literal
                        if (int.TryParse(argValue, out int intValue))
                        {
                            arguments.Add(new LiteralNode(intValue));
                        }
                        else
                        {
                            arguments.Add(new LiteralNode(argValue));
                        }
                    }
                    else if (nextToken.Type == TokenType.Comma)
                    {
                        Advance(); // Consume ','
                    }
                    else
                    {
                        break; // End of arguments or unexpected token
                    }
                }
                
                // Consume ')'
                if (Peek().Type != TokenType.RightParen)
                {
                    throw new ParseException($"Expected ')' at position {Peek().Position}");
                }
                Advance();
            }
            
            return new MethodNode(target, methodName, arguments);
        }

        private AstNode ParseSlice(AstNode target)
        {
            // Consume '['
            Advance();
            
            AstNode? start = null;
            AstNode? end = null;
            
            // Check for start index
            if (Peek().Type == TokenType.Identifier)
            {
                var startValue = Advance().Value;
                if (int.TryParse(startValue, out int intValue))
                {
                    start = new LiteralNode(intValue);
                }
                else
                {
                    throw new ParseException($"Invalid slice start index: {startValue} at position {Peek().Position}");
                }
            }
            
            // Check for ':'
            if (Peek().Type == TokenType.Colon)
            {
                Advance(); // Consume ':'
                
                // Check for end index
                if (Peek().Type == TokenType.Identifier)
                {
                    var endValue = Advance().Value;
                    if (int.TryParse(endValue, out int intValue))
                    {
                        end = new LiteralNode(intValue);
                    }
                    else
                    {
                        throw new ParseException($"Invalid slice end index: {endValue} at position {Peek().Position}");
                    }
                }
            }
            
            // Consume ']'
            if (Peek().Type != TokenType.RightBracket)
            {
                throw new ParseException($"Expected ']' at position {Peek().Position}");
            }
            Advance();
            
            return new SliceNode(target, start, end);
        }

        private Token Peek()
        {
            if (_currentIndex >= _tokens.Count)
                return _tokens[_tokens.Count - 1]; // Return EOF token

            return _tokens[_currentIndex];
        }

        private Token Advance()
        {
            if (!IsAtEnd())
                _currentIndex++;

            return _tokens[_currentIndex - 1];
        }

        private bool IsAtEnd()
        {
            return _currentIndex >= _tokens.Count || 
                   _tokens[_currentIndex].Type == TokenType.EOF;
        }
    }

    /// <summary>
    /// Parse exception
    /// </summary>
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }
}
