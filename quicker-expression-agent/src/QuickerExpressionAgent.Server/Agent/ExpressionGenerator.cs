using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using System.Text.Json;

namespace QuickerExpressionAgent.Server.Agent;

/// <summary>
/// Generates C# expressions from natural language using LLM
/// </summary>
public class ExpressionGenerator
{
    private readonly Kernel _kernel;

    public ExpressionGenerator(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Generate a C# expression from natural language description
    /// Uses new format with variable declarations and separator
    /// </summary>
    public async Task<string> GenerateExpressionAsync(
        string naturalLanguage,
        List<VariableClass>? variableList = null,
        string? previousExpression = null,
        string? previousError = null,
        CancellationToken cancellationToken = default)
    {
        // Build variable list JSON if provided
        string variableListJson = "[]";
        if (variableList != null && variableList.Any())
        {
            var variableListData = variableList.Select(v => new
            {
                VarName = v.VarName,
                VarType = v.VarType.ToString(),  // Convert enum to string for JSON
                DefaultValue = v.DefaultValue
            }).ToList();
            variableListJson = JsonSerializer.Serialize(variableListData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        var prompt = $$$"""
            Convert the following natural language description to a C# expression that can be executed in Quicker.

            ## Task Flow:
            1. **Plan Input/Output**: Analyze the natural language to identify what input variables are needed and what the output should be
            2. **Create Input Variables**: Generate variable declarations for input variables (NOT temporary variables used in the expression)
            3. **Generate Expression**: Generate the expression using curly braces around variable names to reference variables (e.g., {varname})

            ## Expression Format:
            The output must follow this format:
            ```
            [Variable Declarations Section]
            ////-------分隔符-------////
            [Expression Section with {varname} format - use curly braces around variable names]
            ```

            ## Variable Declaration Rules:
            - **Input Variables**: Variables that are needed as input from the user/context should be declared in the variable declarations section
            - **Temporary Variables**: Variables used only within the expression (like loop counters, intermediate calculations) should be declared in the expression section, NOT in the variable declarations section
            - **Explicit Type Declaration**: You MUST use explicit type declarations (NOT `var`). Only the following types are allowed:
              - `string` - for text values
              - `int` - for integer numbers
              - `double` - for decimal numbers
              - `bool` - for boolean values
              - `DateTime` - for date and time values
              - `List<string>` - for lists of strings
              - `Dictionary<string, object>` - for dictionaries with string keys and object values
              - `object` - for any other type (use sparingly)
            - **Variable Declaration Format**: Use explicit type syntax: `TypeName variableName = defaultValue;`
              - String: `string name = "";`
              - Int: `int count = 0;`
              - Double: `double price = 0.0;`
              - Bool: `bool isActive = false;`
              - DateTime: `DateTime date = DateTime.Now;` or `DateTime date = new DateTime();`
              - List<string>: `List<string> items = new List<string>();`
              - Dictionary<string, object>: `Dictionary<string, object> dict = new Dictionary<string, object>();`
              - Object: `object value = null;`

            ## Expression Section Rules:
            - Use curly braces around variable names to reference variables declared in the variable declarations section (e.g., {variableName})
            - **IMPORTANT**: The curly braces {variableName} are just text format, NOT function calls. Write them literally in the output.
            - You can declare temporary variables in the expression section (e.g., `var random = new Random();`)
            - The expression executor will automatically replace {variableName} with the actual variable name
            - Support method calls, arithmetic operations, string operations, LINQ operations
            - Support control flow (if, for, while, etc.) and return statements
            - **Write concise code**: Prefer LINQ expressions over verbose loops when possible
              - Use `.Where()`, `.Select()`, `.Any()`, `.All()`, `.FirstOrDefault()`, `.Count()`, `.Sum()`, `.Aggregate()`, etc.
              - Example: Instead of `for` loop, use `list.Where(x => x > 0).Sum()`
              - Example: Instead of manual filtering, use `items.Where(item => item.Contains("test"))`
            - Keep expressions simple and readable, but leverage LINQ for common operations

            ## Pre-defined Variables (from previous iterations or existing context):
            {{{variableListJson}}}
            - **IMPORTANT**: If variables are already defined above, you MUST reuse them by using curly braces format: {variableName}
            - **DO NOT redeclare variables that are already in the pre-defined list** - just use them in the expression section
            - Only declare NEW variables that are not in the pre-defined list
            - If a variable exists in the pre-defined list, skip its declaration and use {variableName} directly in the expression

            ## Examples:

            Example 1 - Simple calculation:
            Natural Language: "计算两个数的和"
            Output:
            int num1 = 0;
            int num2 = 0;
            ////-------分隔符-------////
            {num1} + {num2}

            Example 2 - Using pre-defined variables (DO NOT redeclare):
            Pre-defined variables: baseValue (int, default: 10)
            Natural Language: "将基础值乘以2"
            Output:
            ////-------分隔符-------////
            {baseValue} * 2

            Example 3 - Mixing pre-defined and new variables:
            Pre-defined variables: name (string, default: "John")
            Natural Language: "拼接姓名和新的年龄"
            Output:
            int age = 0;
            ////-------分隔符-------////
            {name} + " is " + {age} + " years old"

            Natural Language: {{{naturalLanguage}}}
            """;
        
        // Replace the placeholders with actual values
        prompt = prompt.Replace("{{naturalLanguage}}", naturalLanguage);
        prompt = prompt.Replace("{{variableListJson}}", variableListJson);

        if (!string.IsNullOrWhiteSpace(previousExpression))
        {
            prompt += $"\n\n## Previous Expression (for context and correction):\n```\n{previousExpression}\n```";
        }

        if (!string.IsNullOrWhiteSpace(previousError))
        {
            prompt += $"\n\n## Previous Execution Error:\n{previousError}";
            prompt += "\n\nPlease analyze the error, understand what went wrong, and generate a corrected expression based on the previous expression above.";
        }

        prompt += "\n\nGenerate the complete expression following the format above (variable declarations + separator + expression with curly braces around variable names like {variableName}):";

        var function = KernelFunctionFactory.CreateFromPrompt(prompt);
        var result = await _kernel.InvokeAsync(function, cancellationToken: cancellationToken);

        var expression = result.ToString().Trim();
        
        // Remove markdown code block markers if present (```csharp, ```cs, ```, etc.)
        // Check for common code block patterns
        if (expression.StartsWith("```"))
        {
            // Find the first newline after ```
            var newlineIndex = expression.IndexOf('\n');
            if (newlineIndex > 0)
            {
                // Remove the opening ``` and language identifier
                expression = expression.Substring(newlineIndex + 1);
            }
            else
            {
                // No newline, just remove ```
                expression = expression.Substring(3);
            }
        }
        
        // Remove closing ``` if present
        if (expression.EndsWith("```"))
        {
            expression = expression.Substring(0, expression.Length - 3);
        }
        
        expression = expression.Trim();

        return expression;
    }
}

