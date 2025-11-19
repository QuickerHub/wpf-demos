using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Agent;

/// <summary>
/// Parser for the new expression format with variable declarations and separator
/// Converts to {varname} format and VariableClass list
/// Uses Roslyn script execution to extract variables from variable declarations
/// </summary>
public static class ExpressionParser
{
    private const string Separator = "////-------分隔符-------////";
    private static readonly ScriptOptions ScriptOptions = ScriptOptions.Default
        .WithImports("System", "System.Linq", "System.Collections.Generic")
        .WithReferences(
            typeof(object).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            typeof(System.Random).Assembly,
            typeof(System.Console).Assembly,
            typeof(System.Math).Assembly,
            typeof(DateTime).Assembly);

    /// <summary>
    /// Parse expression with variable declarations and convert to {varname} format with VariableClass list
    /// Executes variable declarations to extract variables and their default values
    /// </summary>
    /// <param name="fullExpression">Full expression with variable declarations and separator</param>
    /// <returns>Parsed expression with {varname} format and variable list</returns>
    public static ParsedExpression Parse(string fullExpression)
    {
        var parts = fullExpression.Split(new[] { Separator }, StringSplitOptions.None);
        
        if (parts.Length != 2)
        {
            // No separator found, treat as simple expression with no variables
            return new ParsedExpression
            {
                Expression = fullExpression,
                VariableList = new List<VariableClass>()
            };
        }

        var variableDeclarations = parts[0].Trim();
        var expression = parts[1].Trim();

        // Execute variable declarations to extract variables and their values
        var variableList = ParseVariableDeclarationsByExecution(variableDeclarations);

        // Keep expression as is with {varname} format - Quicker will handle replacement
        return new ParsedExpression
        {
            Expression = expression,
            VariableList = variableList
        };
    }

    /// <summary>
    /// Parse variable declarations by executing them and extracting variables using return new{a,b} pattern
    /// Simple approach: execute variable declarations + return new{var1, var2, ...} to get dictionary
    /// </summary>
    private static List<VariableClass> ParseVariableDeclarationsByExecution(string declarations)
    {
        var variableList = new List<VariableClass>();
        
        if (string.IsNullOrWhiteSpace(declarations))
        {
            return variableList;
        }

        try
        {
            // Extract variable names from declarations (simple regex to find variable names)
            // Pattern: type name = value; or var name = value;
            var varNames = new List<string>();
            var lines = declarations.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;
                
                // Match: type varName = ... or var varName = ...
                var match = System.Text.RegularExpressions.Regex.Match(
                    trimmed, 
                    @"(?:var|string|int|double|bool|DateTime|List<string>|object)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=");
                
                if (match.Success && match.Groups.Count > 1)
                {
                    varNames.Add(match.Groups[1].Value);
                }
            }
            
            if (varNames.Count == 0)
            {
                Console.WriteLine($"[ExpressionParser] No variables found in declarations");
                return variableList;
            }
            
            // Build code: variable declarations + return new{var1, var2, ...}
            var returnStatement = $"return new{{{string.Join(", ", varNames)}}};";
            var fullCode = declarations.Trim() + "\n" + returnStatement;
            
            Console.WriteLine($"[ExpressionParser] Executing code:\n{fullCode}");
            
            // Execute the code
            var script = CSharpScript.Create(fullCode, ScriptOptions);
            var compilation = script.Compile();
            
            if (compilation.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                var errors = string.Join("\n", compilation.Select(d => d.GetMessage()));
                Console.WriteLine($"[ExpressionParser] Compilation failed: {errors}");
                return variableList;
            }

            // Run the script and get the anonymous object
            var state = script.RunAsync().GetAwaiter().GetResult();
            var result = state.ReturnValue;
            
            if (result == null)
            {
                Console.WriteLine($"[ExpressionParser] Execution returned null");
                return variableList;
            }
            
            // Extract properties from anonymous object to get variable values
            // Use reflection to get all properties
            var properties = result.GetType().GetProperties();
            foreach (var property in properties)
            {
                var varName = property.Name;
                var varValue = property.GetValue(result);
                var varType = property.PropertyType.ConvertToVariableType();
                
                variableList.Add(new VariableClass
                {
                    VarName = varName,
                    VarType = varType,
                    DefaultValue = varValue ?? varType.GetDefaultValue()
                });
            }
            
            Console.WriteLine($"[ExpressionParser] Extracted {variableList.Count} variables: {string.Join(", ", variableList.Select(v => $"{v.VarName}={v.DefaultValue}"))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpressionParser] Execution failed: {ex.Message}");
            Console.WriteLine($"[ExpressionParser] Stack trace: {ex.StackTrace}");
            return variableList;
        }

        return variableList;
    }



}

/// <summary>
/// Parsed expression result
/// </summary>
public class ParsedExpression
{
    /// <summary>
    /// Expression content with {varname} format (ready to send to Quicker)
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// List of variable declarations
    /// </summary>
    public List<VariableClass> VariableList { get; set; } = new();
}

