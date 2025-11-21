using System.Text.Json;
using System.Text.RegularExpressions;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Agent;

/// <summary>
/// Parser for final answer that extracts variables and expression from structured format
/// </summary>
public static class FinalAnswerParser
{
    /// <summary>
    /// Parse result from final answer
    /// </summary>
    public class ParseResult
    {
        public bool IsValid { get; set; }
        public List<VariableClass> Variables { get; set; } = new();
        public string Expression { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Parse final answer content to extract variables and expression
    /// Primary format: &lt;InputVarDefine&gt;...&lt;/InputVarDefine&gt; &lt;Expression&gt;...&lt;/Expression&gt;
    /// Fallback format: InputVarDefine: [...] Expression: ... (legacy)
    /// Fallback format: {"variables": [...], "expression": "..."} (JSON)
    /// </summary>
    public static ParseResult Parse(string finalAnswer)
    {
        var result = new ParseResult();

        if (string.IsNullOrWhiteSpace(finalAnswer))
        {
            result.ErrorMessage = "Final answer is empty";
            return result;
        }

        // Priority 1: Try XML tag format <InputVarDefine>...</InputVarDefine> <Expression>...</Expression>
        var inputVarDefineTagMatch = Regex.Match(finalAnswer, @"<InputVarDefine>(.*?)</InputVarDefine>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var expressionTagMatch = Regex.Match(finalAnswer, @"<Expression>(.*?)</Expression>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        if (inputVarDefineTagMatch.Success && expressionTagMatch.Success)
        {
            var varDefineText = inputVarDefineTagMatch.Groups[1].Value.Trim();
            var expressionText = expressionTagMatch.Groups[1].Value.Trim();
            
            // Try to extract JSON array from InputVarDefine
            var jsonArrayMatch = Regex.Match(varDefineText, @"\[.*\]", RegexOptions.Singleline);
            if (jsonArrayMatch.Success && !string.IsNullOrWhiteSpace(expressionText))

            {
                try
                {
                    // Parse variables from InputVarDefine/VarDefine (JSON array)
                    var varJson = jsonArrayMatch.Value;
                    using var varDoc = JsonDocument.Parse(varJson);
                    var varRoot = varDoc.RootElement;

                    if (varRoot.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var varElement in varRoot.EnumerateArray())
                        {
                            try
                            {
                                var varName = varElement.GetProperty("VarName").GetString();
                                var varTypeStr = varElement.GetProperty("VarType").GetString();
                                
                                if (!string.IsNullOrEmpty(varName) && 
                                    Enum.TryParse<VariableType>(varTypeStr, true, out var varType))
                                {
                                    // Handle DefaultValue - can be string, number, bool, array, object, etc.
                                    object? convertedValue = null;
                                    if (varElement.TryGetProperty("DefaultValue", out var defaultValueElement))
                                    {
                                        // Convert JsonElement directly to appropriate .NET type based on VariableType
                                        convertedValue = varType.ConvertValueFromJson(defaultValueElement);
                                    }
                                    
                                    // If no DefaultValue provided, use default for the type
                                    if (convertedValue == null)
                                    {
                                        convertedValue = varType.GetDefaultValue();
                                    }
                                    
                                    var variable = new VariableClass
                                    {
                                        VarName = varName,
                                        VarType = varType
                                    };
                                    variable.SetDefaultValue(convertedValue);
                                    result.Variables.Add(variable);
                                }
                            }
                            catch
                            {
                                // Skip invalid variable entry
                            }
                        }
                    }

                    // Extract expression (trim whitespace but preserve structure)
                    result.Expression = expressionText;

                    // Validate result
                    // Allow empty variable list (some expressions don't need variables)
                    // But expression must not be empty
                    if (!string.IsNullOrWhiteSpace(result.Expression))
                    {
                        result.IsValid = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Expression is empty in <InputVarDefine>/<Expression> format";
                    }
                }
                catch (JsonException ex)
                {
                    result.ErrorMessage = $"Invalid JSON format in <InputVarDefine>: {ex.Message}";
                }
            }
        }
        
        if (!result.IsValid)
        {
            // Fallback 1: Try legacy format InputVarDefine: ... Expression: ...
            var inputVarDefineIndex = finalAnswer.IndexOf("InputVarDefine:", StringComparison.OrdinalIgnoreCase);
            var varDefineIndex = finalAnswer.IndexOf("VarDefine:", StringComparison.OrdinalIgnoreCase);
            var expressionIndex = finalAnswer.IndexOf("Expression:", StringComparison.OrdinalIgnoreCase);
            
            // Prefer InputVarDefine over VarDefine
            var varDefineKeyIndex = inputVarDefineIndex >= 0 ? inputVarDefineIndex : varDefineIndex;
            var varDefineKey = inputVarDefineIndex >= 0 ? "InputVarDefine:" : "VarDefine:";
            
            if (varDefineKeyIndex >= 0 && expressionIndex > varDefineKeyIndex)
            {
                // Extract InputVarDefine/VarDefine content (from "InputVarDefine:" to "Expression:")
                var varDefineStart = varDefineKeyIndex + varDefineKey.Length;
                var varDefineEnd = expressionIndex;
                var varDefineText = finalAnswer.Substring(varDefineStart, varDefineEnd - varDefineStart).Trim();
                
                // Extract Expression content (from "Expression:" to end, but stop at double newline or next keyword)
                var expressionStart = expressionIndex + "Expression:".Length;
                var expressionText = finalAnswer.Substring(expressionStart).Trim();
                
                // Try to find end of expression (double newline or next keyword)
                var expressionEndMatch = Regex.Match(expressionText, @"(.+?)(?:\n\n|\nInputVarDefine:|\nVarDefine:|\n\{|$)");
                if (expressionEndMatch.Success)
                {
                    expressionText = expressionEndMatch.Groups[1].Value.Trim();
                }
                
                // Try to extract JSON array from InputVarDefine/VarDefine
                var jsonArrayMatch = Regex.Match(varDefineText, @"\[.*\]", RegexOptions.Singleline);
                if (jsonArrayMatch.Success && !string.IsNullOrWhiteSpace(expressionText))
                {
                    try
                    {
                        // Parse variables from InputVarDefine/VarDefine (JSON array)
                        var varJson = jsonArrayMatch.Value;
                        using var varDoc = JsonDocument.Parse(varJson);
                        var varRoot = varDoc.RootElement;

                        if (varRoot.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var varElement in varRoot.EnumerateArray())
                            {
                                try
                                {
                                    var varName = varElement.GetProperty("VarName").GetString();
                                    var varTypeStr = varElement.GetProperty("VarType").GetString();
                                    
                                    if (!string.IsNullOrEmpty(varName) && 
                                        Enum.TryParse<VariableType>(varTypeStr, true, out var varType))
                                    {
                                        // Handle DefaultValue - can be string, number, bool, array, object, etc.
                                        object? convertedValue = null;
                                        if (varElement.TryGetProperty("DefaultValue", out var defaultValueElement))
                                        {
                                            // Convert JsonElement directly to appropriate .NET type based on VariableType
                                            convertedValue = varType.ConvertValueFromJson(defaultValueElement);
                                        }
                                        
                                        // If no DefaultValue provided, use default for the type
                                        if (convertedValue == null)
                                        {
                                            convertedValue = varType.GetDefaultValue();
                                        }
                                        
                                        var variable = new VariableClass
                                        {
                                            VarName = varName,
                                            VarType = varType
                                        };
                                        variable.SetDefaultValue(convertedValue);
                                        result.Variables.Add(variable);
                                    }
                                }
                                catch
                                {
                                    // Skip invalid variable entry
                                }
                            }
                        }

                        // Extract expression (trim whitespace but preserve structure)
                        result.Expression = expressionText;

                        // Validate result
                        // Allow empty variable list (some expressions don't need variables)
                        // But expression must not be empty
                        if (!string.IsNullOrWhiteSpace(result.Expression))
                        {
                            result.IsValid = true;
                        }
                        else
                        {
                            result.ErrorMessage = $"Expression is empty in {varDefineKey} format";
                        }
                    }
                    catch (JsonException ex)
                    {
                        result.ErrorMessage = $"Invalid JSON format in {varDefineKey}: {ex.Message}";
                    }
                }
            }
        }
        
        if (!result.IsValid)
        {
            // Fallback 2: Try JSON format: {"variables": [...], "expression": "..."}
            var jsonPattern = @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}";
            var jsonMatch = Regex.Match(finalAnswer, jsonPattern, RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                try
                {
                    var jsonText = jsonMatch.Value;
                    using var doc = JsonDocument.Parse(jsonText);
                    var root = doc.RootElement;

                    // Extract variables
                    if (root.TryGetProperty("variables", out var variablesElement) && 
                        variablesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var varElement in variablesElement.EnumerateArray())
                        {
                            try
                            {
                                var varName = varElement.GetProperty("VarName").GetString();
                                var varTypeStr = varElement.GetProperty("VarType").GetString();
                                var defaultValue = varElement.TryGetProperty("DefaultValue", out var defaultValueElement) 
                                    ? defaultValueElement.GetRawText().Trim('"') 
                                    : null;

                                if (!string.IsNullOrEmpty(varName) && 
                                    Enum.TryParse<VariableType>(varTypeStr, true, out var varType))
                                {
                                    var variable = new VariableClass
                                    {
                                        VarName = varName,
                                        VarType = varType
                                    };
                                    variable.SetDefaultValue(varType.GetDefaultValue(defaultValue));
                                    result.Variables.Add(variable);
                                }
                            }
                            catch
                            {
                                // Skip invalid variable entry
                            }
                        }
                    }

                    // Extract expression
                    if (root.TryGetProperty("expression", out var expressionElement))
                    {
                        result.Expression = expressionElement.GetString() ?? string.Empty;
                    }

                    // Validate result
                    // Allow empty variable list (some expressions don't need variables)
                    // But expression must not be empty
                    if (!string.IsNullOrWhiteSpace(result.Expression))
                    {
                        result.IsValid = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Expression is empty in JSON format";
                    }
                }
                catch (JsonException ex)
                {
                    result.ErrorMessage = $"Invalid JSON format: {ex.Message}";
                }
            }
            else
            {
                result.ErrorMessage = "Final answer does not contain valid structured format (<InputVarDefine>/<Expression> XML tags, InputVarDefine/Expression format, or JSON)";
            }
        }

        return result;
    }
}

