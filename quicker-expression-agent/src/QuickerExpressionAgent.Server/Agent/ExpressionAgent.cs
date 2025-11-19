using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Communication;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Server.Agent;

/// <summary>
/// Agent that generates and refines C# expressions from natural language
/// </summary>
public class ExpressionAgent
{
    private readonly Kernel _kernel;
    private readonly ExpressionGenerator _generator;
    private readonly QuickerServiceClient? _quickerClient;
    private readonly IRoslynExpressionService _roslynService;

    public ExpressionAgent(
        Kernel kernel, 
        ExpressionGenerator generator, 
        QuickerServiceClient? quickerClient = null,
        IRoslynExpressionService? roslynService = null)
    {
        _kernel = kernel;
        _generator = generator;
        _quickerClient = quickerClient;
        _roslynService = roslynService ?? new RoslynExpressionService();
    }

    /// <summary>
    /// Callback for reporting attempt progress
    /// </summary>
    public delegate void AttemptProgressCallback(ExpressionAttempt attempt);

    /// <summary>
    /// Generate and refine a C# expression from natural language with automatic error correction
    /// </summary>
    public async Task<ExpressionGenerationResult> GenerateAndRefineExpressionAsync(
        string naturalLanguage,
        Dictionary<string, object>? testVariables = null,
        List<VariableClass>? existingVariables = null,
        int maxAttempts = 3,
        AttemptProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        string? expression = null;
        string? lastError = null;
        var attempts = new List<ExpressionAttempt>();

        // Start with existing variables if provided
        List<VariableClass>? currentVariableList = existingVariables;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate or refine expression
            // Pass previous expression and error to AI for correction
            expression = await _generator.GenerateExpressionAsync(
                naturalLanguage,
                currentVariableList,
                expression,  // Previous expression as context
                lastError,   // Previous error for correction
                cancellationToken);

            // Parse the expression to handle variable declarations and separator
            var parsedExpression = ExpressionParser.Parse(expression);

            // Update variable list from parsed expression
            currentVariableList = parsedExpression.VariableList;

            // Test the expression
            ExpressionResult testResult;
            try
            {
                // Use Roslyn service if Quicker client is not available (debug mode)
                if (_quickerClient == null)
                {
                    Console.WriteLine($"[ExpressionAgent] Attempt {i + 1}: Using Roslyn service for execution");
                    Console.WriteLine($"[ExpressionAgent] Parsed expression: {parsedExpression.Expression}");
                    Console.WriteLine($"[ExpressionAgent] Variable count: {parsedExpression.VariableList.Count}");
                    
                    testResult = await _roslynService.ExecuteExpressionAsync(
                        parsedExpression.Expression,
                        parsedExpression.VariableList);
                }
                else
                {
                    Console.WriteLine($"[ExpressionAgent] Attempt {i + 1}: Using Quicker service for execution");
                    testResult = await _quickerClient.ExecuteExpressionAsync(
                        parsedExpression.Expression,
                        parsedExpression.VariableList);
                }

                var attempt = new ExpressionAttempt
                {
                    Attempt = i + 1,
                    Expression = expression,
                    ParsedExpression = parsedExpression.Expression,
                    VariableList = parsedExpression.VariableList,
                    Success = testResult.Success,
                    Result = testResult.Value?.ToString(),
                    Error = testResult.Error
                };
                attempts.Add(attempt);

                // Report progress immediately
                progressCallback?.Invoke(attempt);

                if (testResult.Success)
                {
                    return new ExpressionGenerationResult
                    {
                        Success = true,
                        Expression = expression,
                        ParsedExpression = parsedExpression.Expression,
                        VariableList = parsedExpression.VariableList,
                        Result = testResult.Value,
                        Attempts = attempts
                    };
                }

                // Execution failed, prepare error for next iteration
                lastError = testResult.Error ?? "Unknown error";
                Console.WriteLine($"[ExpressionAgent] Attempt {i + 1} failed: {lastError}");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                var attempt = new ExpressionAttempt
                {
                    Attempt = i + 1,
                    Expression = expression,
                    ParsedExpression = parsedExpression.Expression,
                    VariableList = parsedExpression.VariableList,
                    Success = false,
                    Error = ex.Message
                };
                attempts.Add(attempt);
                
                // Report progress immediately
                progressCallback?.Invoke(attempt);
                
                Console.WriteLine($"[ExpressionAgent] Attempt {i + 1} exception: {ex.Message}");
            }
        }

        // Return the last attempt even if it failed
        return new ExpressionGenerationResult
        {
            Success = false,
            Expression = expression ?? string.Empty,
            Error = lastError,
            Attempts = attempts
        };
    }
}

/// <summary>
/// Result of expression generation
/// </summary>
public class ExpressionGenerationResult
{
    public bool Success { get; set; }
    public string Expression { get; set; } = string.Empty;
    public string ParsedExpression { get; set; } = string.Empty;
    public List<VariableClass> VariableList { get; set; } = new();
    public object? Result { get; set; }
    public string? Error { get; set; }
    public List<ExpressionAttempt> Attempts { get; set; } = new();
}

/// <summary>
/// Information about a single generation attempt
/// </summary>
public class ExpressionAttempt
{
    public int Attempt { get; set; }
    public string Expression { get; set; } = string.Empty;
    public string ParsedExpression { get; set; } = string.Empty;
    public List<VariableClass> VariableList { get; set; } = new();
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}

