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

            // Look ahead to find colon and right brace
            int colonIndex = -1;
            int rightBraceIndex = -1;
            for (int i = _currentIndex; i < _tokens.Count; i++)
            {
                if (_tokens[i].Type == TokenType.RightBrace)
                {
                    rightBraceIndex = i;
                    break;
                }
                if (_tokens[i].Type == TokenType.Colon && colonIndex == -1)
                {
                    colonIndex = i;
                }
            }
            
            // Build content string (either before colon, or before '}' if no colon)
            int contentEndIndex = colonIndex > _currentIndex ? colonIndex : rightBraceIndex;
            if (contentEndIndex > _currentIndex)
            {
                var contentSb = new System.Text.StringBuilder();
                for (int i = _currentIndex; i < contentEndIndex; i++)
                {
                    contentSb.Append(_tokens[i].Value);
                }
                var contentStr = contentSb.ToString();
                
                // Check if content looks like an expression
                // Expression format should NOT contain method calls ('.' or '(') or slices ('[')
                // It should only contain variable names and operators
                bool isExpressionFormat = false;
                if (contentStr.Length > 0)
                {
                    // Check if content contains method calls or slices - if so, it's NOT an expression
                    bool hasMethodCall = false;
                    for (int i = _currentIndex; i < contentEndIndex; i++)
                    {
                        if (_tokens[i].Type == TokenType.Dot || 
                            _tokens[i].Type == TokenType.LeftParen ||
                            _tokens[i].Type == TokenType.LeftBracket)
                        {
                            hasMethodCall = true;
                            break;
                        }
                    }
                    
                    if (!hasMethodCall)
                    {
                        // Starts with digit -> expression (e.g., "2i+1")
                        if (char.IsDigit(contentStr[0]))
                        {
                            isExpressionFormat = true;
                        }
                        // Contains operators -> expression (e.g., "i*2+1", "i2+1")
                        else if (contentStr.Contains("+") || contentStr.Contains("-") || 
                                 contentStr.Contains("*") || contentStr.Contains("/"))
                        {
                            // But exclude single 'i' (which is {i:format} or {i})
                            if (contentStr.ToLower() != "i")
                            {
                                isExpressionFormat = true;
                            }
                        }
                    }
                }
                
                if (isExpressionFormat)
                {
                    // Parse as expression format: {2i+1:00} or {i2+1}
                    string expressionString;
                    string formatString;
                    
                    if (colonIndex > _currentIndex)
                    {
                        // Has colon: {2i+1:00}
                        var (expr, fmt) = ReadExpressionAndFormatFromStart();
                        expressionString = expr;
                        formatString = fmt;
                    }
                    else
                    {
                        // No colon: {i2+1}
                        var exprSb = new System.Text.StringBuilder();
                        while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
                        {
                            var token = Advance();
                            exprSb.Append(token.Value);
                        }
                        expressionString = exprSb.ToString();
                        formatString = string.Empty; // No format string
                    }
                    
                    var dummyVariableNode = new VariableNode("i"); // Dummy node for FormatNode
                    
                    // Consume '}'
                    if (Peek().Type != TokenType.RightBrace)
                    {
                        throw new ParseException($"Expected '}}' at position {Peek().Position}");
                    }
                    Advance();
                    
                    return new FormatNode(dummyVariableNode, expressionString, formatString);
                }
            }

            // Parse variable first (format specifier applies to variables, not method calls)
            AstNode node = ParseVariable();

            // Check for format specifier (colon after variable)
            // Format specifier is universal for all variables, not just 'i'
            if (Peek().Type == TokenType.Colon && node is VariableNode)
            {
                Advance(); // Consume ':'
                
                // Read format string until '}' or method call/slice
                // For variable format, we don't support expression (like {i:2*i+1:000})
                // Only support simple format: {variable:format}
                string formatString = ReadFormatString();
                
                node = new FormatNode(node, null, formatString);
            }
            
            // Parse method calls and slices (chain them) - can be after format or directly after variable
            node = ParseMethodCallsAndSlices(node);

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

        /// <summary>
        /// Read expression and format from the start of expression (for {2i+1:00} syntax)
        /// </summary>
        private (string expressionString, string formatString) ReadExpressionAndFormatFromStart()
        {
            var expressionSb = new System.Text.StringBuilder();
            var formatSb = new System.Text.StringBuilder();
            bool foundColon = false;
            
            // Read until '}' or ':' or method call/slice
            while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
            {
                var token = Peek();
                
                // Check if this is a colon (separating expression and format)
                if (token.Type == TokenType.Colon && !foundColon)
                {
                    // Found colon, switch to reading format string
                    foundColon = true;
                    Advance(); // Consume ':'
                    continue;
                }
                
                // Handle dot: could be part of format string (.2f) or method call (.upper)
                if (token.Type == TokenType.Dot)
                {
                    // Peek ahead to see if next token is a number/text (format string) or identifier (method call)
                    if (_currentIndex + 1 < _tokens.Count)
                    {
                        var nextToken = _tokens[_currentIndex + 1];
                        // If next token is identifier, it's a method call - stop reading format string
                        if (nextToken.Type == TokenType.Identifier)
                        {
                            break;
                        }
                        // Otherwise, it's part of format string (like .2f) - include it
                    }
                    // If no next token or next token is not identifier, include the dot
                    var advancedToken = Advance();
                    if (foundColon)
                    {
                        formatSb.Append(advancedToken.Value);
                    }
                    else
                    {
                        expressionSb.Append(advancedToken.Value);
                    }
                    continue;
                }
                
                // Stop if we encounter slice (they come after format)
                if (token.Type == TokenType.LeftBracket)
                {
                    break;
                }
                
                var advancedToken2 = Advance();
                
                // Append to expression or format based on whether we found colon
                if (foundColon)
                {
                    formatSb.Append(advancedToken2.Value);
                }
                else
                {
                    expressionSb.Append(advancedToken2.Value);
                }
            }

            var expressionString = expressionSb.ToString();
            var formatString = formatSb.ToString();
            
            if (!foundColon)
            {
                throw new ParseException($"Expected ':' in expression format at position {Peek().Position}");
            }
            
            return (expressionString, formatString);
        }

        private (string? expressionString, string formatString) ReadExpressionAndFormat()
        {
            var expressionSb = new System.Text.StringBuilder();
            var formatSb = new System.Text.StringBuilder();
            bool foundSecondColon = false;
            
            // Read until '}' or second ':' or method call/slice
            while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
            {
                var token = Peek();
                
                // Check if this is a second colon (separating expression and format)
                if (token.Type == TokenType.Colon && !foundSecondColon)
                {
                    // Found second colon, switch to reading format string
                    foundSecondColon = true;
                    Advance(); // Consume second ':'
                    continue;
                }
                
                // Handle dot: could be part of format string (.2f) or method call (.upper)
                if (token.Type == TokenType.Dot)
                {
                    // Peek ahead to see if next token is a number/text (format string) or identifier (method call)
                    if (_currentIndex + 1 < _tokens.Count)
                    {
                        var nextToken = _tokens[_currentIndex + 1];
                        // If next token is identifier, it's a method call - stop reading format string
                        if (nextToken.Type == TokenType.Identifier)
                        {
                            break;
                        }
                        // Otherwise, it's part of format string (like .2f) - include it
                    }
                    // If no next token or next token is not identifier, include the dot
                    var advancedToken = Advance();
                    if (foundSecondColon)
                    {
                        formatSb.Append(advancedToken.Value);
                    }
                    else
                    {
                        expressionSb.Append(advancedToken.Value);
                    }
                    continue;
                }
                
                // Stop if we encounter slice (they come after format)
                if (token.Type == TokenType.LeftBracket)
                {
                    break;
                }
                
                var advancedToken2 = Advance();
                
                // Append to expression or format based on whether we found second colon
                if (foundSecondColon)
                {
                    formatSb.Append(advancedToken2.Value);
                }
                else
                {
                    expressionSb.Append(advancedToken2.Value);
                }
            }

            var expressionString = expressionSb.ToString();
            var formatString = formatSb.ToString();
            
            // If we found a second colon, return expression and format
            if (foundSecondColon)
            {
                return (expressionString, formatString);
            }
            
            // No second colon found, treat entire content as format string (backward compatibility)
            return (null, expressionString);
        }

        private string ReadFormatString()
        {
            var sb = new System.Text.StringBuilder();
            
            // Read format string until '}' or method call/slice
            while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
            {
                var token = Peek();
                
                // Handle dot: could be part of format string (.2f) or method call (.upper)
                if (token.Type == TokenType.Dot)
                {
                    // Peek ahead to see if next token is a number/text (format string) or identifier (method call)
                    if (_currentIndex + 1 < _tokens.Count)
                    {
                        var nextToken = _tokens[_currentIndex + 1];
                        // If next token is identifier, it's a method call - stop reading format string
                        if (nextToken.Type == TokenType.Identifier)
                        {
                            break;
                        }
                        // Otherwise, it's part of format string (like .2f) - include it
                    }
                    // If no next token or next token is not identifier, include the dot
                    Advance();
                    sb.Append(token.Value);
                    continue;
                }
                
                // Stop if we encounter slice (they come after format)
                if (token.Type == TokenType.LeftBracket)
                {
                    break;
                }
                
                Advance();
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
                
                // Parse arguments: arg1, arg2, arg3
                while (!IsAtEnd())
                {
                    var nextToken = Peek();
                    
                    // End of arguments
                    if (nextToken.Type == TokenType.RightParen || nextToken.Type == TokenType.RightBrace)
                    {
                        break;
                    }
                    
                    // Skip commas
                    if (nextToken.Type == TokenType.Comma)
                    {
                        Advance(); // Consume ','
                        continue;
                    }
                    
                    // Parse argument value
                    AstNode argument;
                    if (nextToken.Type == TokenType.StringLiteral)
                    {
                        var argValue = Advance().Value;
                        argument = new LiteralNode(argValue);
                    }
                    else if (nextToken.Type == TokenType.Identifier)
                    {
                        var argValue = Advance().Value;
                        // Try to parse as number, otherwise treat as string
                        if (int.TryParse(argValue, out int intValue))
                        {
                            argument = new LiteralNode(intValue);
                        }
                        else
                        {
                            argument = new LiteralNode(argValue);
                        }
                    }
                    else if (nextToken.Type == TokenType.Text)
                    {
                        // Text token might be a number (e.g., "10", "1", "3")
                        var argValue = Advance().Value;
                        if (int.TryParse(argValue, out int intValue))
                        {
                            argument = new LiteralNode(intValue);
                        }
                        else
                        {
                            // Not a number, treat as string
                            argument = new LiteralNode(argValue);
                        }
                    }
                    else
                    {
                        // Unexpected token
                        throw new ParseException($"Unexpected token {nextToken.Type} in method arguments at position {nextToken.Position}. Expected Identifier, StringLiteral, or Text.");
                    }
                    
                    // Add argument
                    arguments.Add(argument);
                    
                    // Check what follows: comma (more args) or ')' (end)
                    var next = Peek();
                    if (next.Type == TokenType.Comma)
                    {
                        Advance(); // Consume ',' and continue
                    }
                    else if (next.Type == TokenType.RightParen || next.Type == TokenType.RightBrace)
                    {
                        // End of arguments
                        break;
                    }
                    else
                    {
                        throw new ParseException($"Unexpected token {next.Type} after argument at position {next.Position}. Expected comma or ')'.");
                    }
                }
                
                // Consume ')'
                if (Peek().Type != TokenType.RightParen)
                {
                    throw new ParseException($"Expected ')' at position {Peek().Position}, got {Peek().Type}");
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
            if (Peek().Type == TokenType.Identifier || Peek().Type == TokenType.Text)
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
            else if (Peek().Type == TokenType.StringLiteral)
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
                if (Peek().Type == TokenType.Identifier || Peek().Type == TokenType.Text)
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
                else if (Peek().Type == TokenType.StringLiteral)
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
            
            // Convert slice syntax to method call: .slice(start, end)
            // [:3] -> .slice(0, 3)  (start is null, add 0)
            // [3:] -> .slice(3)    (end is null, only start)
            // [1:3] -> .slice(1, 3) (both start and end)
            // [:] -> .slice()      (both null, no arguments - return full string)
            var arguments = new List<AstNode>();
            
            if (start == null && end != null)
            {
                // [:3] -> .slice(0, 3)
                arguments.Add(new LiteralNode(0));
                arguments.Add(end);
            }
            else if (start != null && end == null)
            {
                // [3:] -> .slice(3)
                arguments.Add(start);
            }
            else if (start != null && end != null)
            {
                // [1:3] -> .slice(1, 3)
                arguments.Add(start);
                arguments.Add(end);
            }
            // else: [:] -> .slice() - no arguments, return full string
            
            // Return MethodNode instead of SliceNode
            return new MethodNode(target, "slice", arguments);
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
