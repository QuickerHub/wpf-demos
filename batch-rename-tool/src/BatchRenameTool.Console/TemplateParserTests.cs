using System;
using System.Linq;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Template.Lexer;
using BatchRenameTool.Template.Ast;

namespace BatchRenameTool
{
    /// <summary>
    /// Tests for template parser with method calls
    /// </summary>
    public class TemplateParserTests
    {
        public void RunAllTests()
        {
            Console.WriteLine("=== Template Parser Method Tests ===");
            Console.WriteLine();

            TestLexer();
            Console.WriteLine();

            TestParser();
            Console.WriteLine();

            TestEvaluator();
            Console.WriteLine();

            Console.WriteLine("All template parser tests completed!");
        }

        private void TestLexer()
        {
            Console.WriteLine("--- Lexer Tests ---");

            var testCases = new[]
            {
                "{name.replace('e','E')}",
                "{name.padLeft(10,'0')}",
                "{name.sub(1,3)}",
                "{name.replace('e','E').upper()}",
            };

            foreach (var input in testCases)
            {
                Console.WriteLine($"\nInput: {input}");
                try
                {
                    var lexer = new TemplateLexer(input);
                    var tokens = lexer.Tokenize();

                    Console.WriteLine("Tokens:");
                    foreach (var token in tokens)
                    {
                        Console.WriteLine($"  {token.Type,-20} '{token.Value}' (pos: {token.Position})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: {ex.Message}");
                }
            }
        }

        private void TestParser()
        {
            Console.WriteLine("--- Parser Tests ---");

            var parser = new TemplateParser(Enumerable.Empty<Type>());

            var testCases = new[]
            {
                "{name.replace('e','E')}",
                "{name.padLeft(10,'0')}",
                "{name.sub(1,3)}",
                "{name.replace('e','E').upper()}",
                "{name.sub(1).padLeft(10,'*')}",
            };

            foreach (var template in testCases)
            {
                Console.WriteLine($"\nTemplate: {template}");
                try
                {
                    var node = parser.Parse(template);
                    Console.WriteLine($"  Parsed successfully: {node.GetType().Name}");
                    
                    // Print AST structure
                    PrintAst(node, "  ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: {ex.Message}");
                    Console.WriteLine($"  Stack: {ex.StackTrace}");
                }
            }
        }

        private void TestEvaluator()
        {
            Console.WriteLine("--- Evaluator Tests ---");

            var parser = new TemplateParser(Enumerable.Empty<Type>());
            var evaluator = new TemplateEvaluator();

            var testCases = new[]
            {
                ("{name.replace('e','E')}", "test", "txt", 0, "tEst"),
                ("{name.padLeft(10,'0')}", "test", "txt", 0, "000000test"),
                ("{name.padRight(10,'-')}", "test", "txt", 0, "test------"),
                ("{name.sub(1,3)}", "test", "txt", 0, "es"),
                ("{name.sub(1)}", "test", "txt", 0, "est"),
                ("{name.replace('e','E').upper()}", "test", "txt", 0, "TEST"),
                ("{name.sub(1).padLeft(10,'*')}", "test", "txt", 0, "*******est"),
                ("{name.upper()}", "test", "txt", 0, "TEST"),
                ("{name.lower()}", "TEST", "txt", 0, "test"),
                ("{name.trim()}", "  test  ", "txt", 0, "test"),
            };

            int passed = 0;
            int failed = 0;

            foreach (var (template, name, ext, index, expected) in testCases)
            {
                Console.WriteLine($"\nTemplate: {template}");
                Console.WriteLine($"  Input: name='{name}', ext='{ext}', index={index}");
                Console.WriteLine($"  Expected: '{expected}'");

                try
                {
                    var context = new EvaluationContext
                    {
                        Name = name,
                        Ext = ext,
                        FullName = $"{name}.{ext}",
                        Index = index
                    };

                    var node = parser.Parse(template);
                    var result = evaluator.Evaluate(node, context);

                    Console.WriteLine($"  Got:      '{result}'");

                    if (result == expected)
                    {
                        Console.WriteLine($"  Status:   ✓ PASS");
                        passed++;
                    }
                    else
                    {
                        Console.WriteLine($"  Status:   ✗ FAIL");
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Status:   ✗ ERROR");
                    Console.WriteLine($"  Exception: {ex.Message}");
                    Console.WriteLine($"  Stack: {ex.StackTrace}");
                    failed++;
                }
            }

            Console.WriteLine($"\n--- Summary ---");
            Console.WriteLine($"Total: {testCases.Length}, Passed: {passed}, Failed: {failed}");
        }

        private void PrintAst(AstNode node, string indent)
        {
            Console.WriteLine($"{indent}{node.GetType().Name}");

            switch (node)
            {
                case TemplateNode templateNode:
                    foreach (var child in templateNode.Nodes)
                    {
                        PrintAst(child, indent + "  ");
                    }
                    break;

                case MethodNode methodNode:
                    Console.WriteLine($"{indent}  Method: {methodNode.MethodName}");
                    Console.WriteLine($"{indent}  Target:");
                    PrintAst(methodNode.Target, indent + "    ");
                    Console.WriteLine($"{indent}  Arguments ({methodNode.Arguments.Count}):");
                    for (int i = 0; i < methodNode.Arguments.Count; i++)
                    {
                        Console.WriteLine($"{indent}    [{i}]:");
                        PrintAst(methodNode.Arguments[i], indent + "      ");
                    }
                    break;

                case VariableNode variableNode:
                    Console.WriteLine($"{indent}  Variable: {variableNode.VariableName}");
                    break;

                case FormatNode formatNode:
                    Console.WriteLine($"{indent}  Format: '{formatNode.FormatString}'");
                    Console.WriteLine($"{indent}  Expression: '{formatNode.ExpressionString}'");
                    Console.WriteLine($"{indent}  Inner:");
                    PrintAst(formatNode.InnerNode, indent + "    ");
                    break;

                case LiteralNode literalNode:
                    Console.WriteLine($"{indent}  Value: {literalNode.Value} ({literalNode.Value?.GetType().Name})");
                    break;

                case TextNode textNode:
                    Console.WriteLine($"{indent}  Text: '{textNode.Text}'");
                    break;
            }
        }
    }
}
