using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Extensions;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Server;

class Program
{
    static async Task Main(string[] args)
    {
        // Create host to run IHostedService
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddServerServices();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Start hosted services (including QuickerServerClientConnector)
            await host.StartAsync();

            // Get services from DI
            var agent = host.Services.GetRequiredService<ExpressionAgent>();
            var executor = host.Services.GetRequiredService<IExpressionExecutor>();

            // Parse command line arguments
            bool runTests = args.Contains("-t") || args.Contains("--test");
            
            if (runTests)
            {
                // Run expression tests
                await TestExpressionExecutorAsync(executor, logger);
            }
            else
            {
                // Default: run expression dialog
                await RunExpressionDialogAsync(agent, logger);
            }

            // Stop hosted services
            await host.StopAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error occurred");
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            host.Dispose();
        }
    }

    /// <summary>
    /// Run interactive expression dialog
    /// </summary>
    private static async Task RunExpressionDialogAsync(ExpressionAgent agent, ILogger logger)
    {
        Console.WriteLine("=== Quicker Expression Agent ===");
        Console.WriteLine("Enter natural language descriptions to generate C# expressions.");
        Console.WriteLine("Type 'exit' to quit.\n");

        // Interactive loop
        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            try
            {
                await agent.GenerateExpressionWithConsoleOutputAsync(userInput);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating expression");
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }

        Console.WriteLine("Goodbye!");
    }

    /// <summary>
    /// Print all available tools/functions as JSON
    /// </summary>
    private static void PrintAvailableTools(Kernel kernel, ILogger logger)
    {
        try
        {
            var tools = new List<object>();
            
            foreach (var plugin in kernel.Plugins)
            {
                foreach (var function in plugin)
                {
                    var parameters = new List<object>();
                    foreach (var parameter in function.Metadata.Parameters)
                    {
                        parameters.Add(new
                        {
                            name = parameter.Name,
                            description = parameter.Description ?? string.Empty,
                            type = parameter.ParameterType?.Name ?? "string",
                            isRequired = parameter.IsRequired,
                            defaultValue = parameter.DefaultValue?.ToString() ?? null
                        });
                    }
                    
                    tools.Add(new
                    {
                        pluginName = plugin.Name,
                        functionName = function.Name,
                        description = function.Description ?? string.Empty,
                        parameters = parameters
                    });
                }
            }
            
            var json = JsonSerializer.Serialize(tools, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            
            Console.WriteLine("\n=== Available Tools/Functions ===");
            Console.WriteLine(json);
            Console.WriteLine("===================================\n");
            
            logger.LogInformation("Available tools: {Count}", tools.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error printing available tools");
            Console.WriteLine($"Error printing tools: {ex.Message}");
        }
    }

    private static async Task RunInteractiveModeAsync(ExpressionAgent agent, ILogger logger)
    {
        Console.WriteLine("Quicker Expression Agent - Interactive Mode");
        Console.WriteLine("Enter natural language descriptions to generate C# expressions.");
        Console.WriteLine("Type 'exit' to quit.\n");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            await GenerateExpressionAsync(agent, input, logger);
            Console.WriteLine();
        }
    }

    private static async Task GenerateExpressionAsync(ExpressionAgent agent, string description, ILogger logger)
    {
        try
        {
            logger.LogInformation("Generating expression for: {Description}", description);

            await agent.GenerateExpressionAsync(
                description,
                progressCallback: (step, content) =>
                {
                    Console.WriteLine($"[{step.Type}] {content}");
                },
                streamingCallback: (stepType, partialContent, isComplete) =>
                {
                    if (isComplete)
                    {
                        Console.WriteLine($"\n✓ Completed: {partialContent}");
                    }
                });

            Console.WriteLine("\nExpression generation completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating expression");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate expression with direct console output using agent (no callbacks, prints directly to console)
    /// </summary>
    private static async Task GenerateExpressionWithConsoleOutputAsync(
        ExpressionAgent agent,
        string naturalLanguage,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Generating expression for: {Description}", naturalLanguage);
            
            await agent.GenerateExpressionWithConsoleOutputAsync(naturalLanguage, cancellationToken);
            
            Console.WriteLine("\nExpression generation completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating expression");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Test expression executor
    /// </summary>
    private static async Task TestExpressionExecutorAsync(IExpressionExecutor executor, ILogger logger)
    {
        Console.WriteLine("=== Expression Executor Test ===\n");
        
        // Test cases
        var testCases = new[]
        {
            new
            {
                Name = "Simple arithmetic",
                Code = "1 + 2 * 3",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "String concatenation",
                Code = "\"Hello\" + \" \" + \"World\"",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Random number generation",
                Code = @"var random = new Random();
random.Next(1, 101)",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Expression with variables",
                Code = "name + \" is \" + age + \" years old\"",
                Variables = new List<VariableClass>
                {
                    new VariableClass { VarName = "name", VarType = VariableType.String, DefaultValue = "Alice" },
                    new VariableClass { VarName = "age", VarType = VariableType.Int, DefaultValue = 25 }
                }
            },
            new
            {
                Name = "List operations",
                Code = @"var list = new List<int> { 1, 2, 3, 4, 5 };
list.Where(x => x > 2).Sum()",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Math operations",
                Code = "Math.Sqrt(16) + Math.Pow(2, 3)",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Expression with {varname} format - predefined variable",
                Code = "{name} + \" is \" + {age}",
                Variables = new List<VariableClass>
                {
                    new VariableClass { VarName = "name", VarType = VariableType.String, DefaultValue = "John" },
                    new VariableClass { VarName = "age", VarType = VariableType.Int, DefaultValue = 25 }
                }
            },
            new
            {
                Name = "Expression with {varname} format - variable in expression",
                Code = @"var list = new List<int> { 1, 2, 3, 4, 5 };
list.Sum()",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Expression with {varname} format - mixed",
                Code = @"var prefix = ""Result: "";
prefix + {value}",
                Variables = new List<VariableClass>
                {
                    new VariableClass { VarName = "value", VarType = VariableType.Int, DefaultValue = 42 }
                }
            },
            new
            {
                Name = "For loop with return statement handling",
                Code = @"var str1 = ""string1"";
var str2 = ""string2"";
var maxLength = Math.Max(str1.Length, str2.Length);
var distance = 0;
for (int i = 0; i < Math.Min(str1.Length, str2.Length); i++)
{
    if (str1[i] != str2[i]) distance++;
}
distance += Math.Abs(str1.Length - str2.Length);
1.0 - (double)distance / maxLength",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Simple return statement",
                Code = @"return 42;",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "If with early return",
                Code = @"var value = 10;
if (value > 5) return value * 2;
value * 3",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "Multiple return statements",
                Code = @"var x = 5;
if (x < 0) return 0;
if (x > 10) return 100;
x * 2",
                Variables = new List<VariableClass>()
            },
            new
            {
                Name = "String similarity with early return",
                Code = @"var str1 = ""string1"";
var str2 = ""string2"";
if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2)) return 0.0;
var maxLength = Math.Max(str1.Length, str2.Length);
var distance = 0;
for (int i = 0; i < Math.Min(str1.Length, str2.Length); i++)
{
    if (str1[i] != str2[i]) distance++;
}
distance += Math.Abs(str1.Length - str2.Length);
1.0 - (double)distance / maxLength",
                Variables = new List<VariableClass>()
            }
        };

        int successCount = 0;
        int failCount = 0;

        foreach (var testCase in testCases)
        {
            Console.WriteLine($"\nTest: {testCase.Name}");
            Console.WriteLine($"Code: {testCase.Code.Replace("\n", "\\n")}");
            if (testCase.Variables.Any())
            {
                Console.WriteLine($"Variables: {string.Join(", ", testCase.Variables.Select(v => $"{v.VarName}={v.DefaultValue}"))}");
            }
            
            try
            {
                var result = await executor.ExecuteExpressionAsync(testCase.Code, testCase.Variables);
                
                if (result.Success)
                {
                    Console.WriteLine($"✓ Success: {result.Value} (Type: {result.Value?.GetType().Name ?? "null"})");
                    successCount++;
                }
                else
                {
                    Console.WriteLine($"✗ Failed: {result.Error}");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Exception: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"\n=== Test Summary ===");
        Console.WriteLine($"Total: {testCases.Length}");
        Console.WriteLine($"Success: {successCount}");
        Console.WriteLine($"Failed: {failCount}");
        Console.WriteLine($"Success Rate: {(successCount * 100.0 / testCases.Length):F1}%");
    }
}

