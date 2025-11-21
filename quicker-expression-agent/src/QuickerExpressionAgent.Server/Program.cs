using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Extensions;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Server;

class Program
{
    /// <summary>
    /// Helper method to create VariableClass for testing
    /// </summary>
    private static VariableClass CreateTestVariable(string name, VariableType type, object? defaultValue)
    {
        var variable = new VariableClass
        {
            VarName = name,
            VarType = type
        };
        variable.SetDefaultValue(defaultValue);
        return variable;
    }

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
            var connector = host.Services.GetRequiredService<QuickerServerClientConnector>();

            // Parse command line arguments
            bool runTests = args.Contains("-t") || args.Contains("--test");
            bool testPlugin = args.Contains("-p") || args.Contains("--test-plugin");
            
            if (testPlugin)
            {
                // Test plugin parameter conversion
                await TestPluginParameterConversionAsync(logger);
            }
            else if (runTests)
            {
                // Run expression tests
                await TestExpressionExecutorAsync(executor, logger);
            }
            else
            {
                // Create or use existing Quicker Code Editor handler
                var toolHandler = await CreateOrGetQuickerCodeEditorHandlerAsync(connector, logger);
                if (toolHandler != null)
                {
                    agent.ToolHandler = toolHandler;
                    logger.LogInformation("Using Quicker Code Editor handler with window handle: {WindowHandle}", toolHandler.WindowHandle);
                }
                else
                {
                    logger.LogWarning("No Quicker Code Editor handler available, using default handler");
                }

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
                    CreateTestVariable("name", VariableType.String, "Alice"),
                    CreateTestVariable("age", VariableType.Int, 25)
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
                    CreateTestVariable("name", VariableType.String, "John"),
                    CreateTestVariable("age", VariableType.Int, 25)
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
                    CreateTestVariable("value", VariableType.Int, 42)
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
                Console.WriteLine($"Variables: {string.Join(", ", testCase.Variables.Select(v => $"{v.VarName}={v.GetDefaultValue()}"))}");
            }
            
            try
            {
                var result = await executor.ExecuteExpressionAsync(testCase.Code, testCase.Variables);
                
                if (result.Success)
                {
                    // Use ValueJson for display (Value is JsonIgnore, not serialized)
                    var valueStr = string.IsNullOrWhiteSpace(result.ValueJson) ? "null" : result.ValueJson;
                    var typeStr = string.IsNullOrWhiteSpace(result.ValueType) ? "unknown" : result.ValueType;
                    Console.WriteLine($"✓ Success: {valueStr} (Type: {typeStr})");
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

    /// <summary>
    /// Test plugin parameter conversion (simulating SemanticKernel behavior)
    /// </summary>
    private static async Task TestPluginParameterConversionAsync(ILogger logger)
    {
        Console.WriteLine("=== Plugin Parameter Conversion Test ===\n");
        
        try
        {
            // Simulate the JSON that SemanticKernel would send
            var jsonArguments = """
                {
                    "expression": "{dict}.Where(kv => kv.Key.StartsWith(\"var\")).ToDictionary(kv => kv.Key, kv => kv.Value)",
                    "variables": [
                        {
                            "VarName": "dict",
                            "VarType": "Dictionary",
                            "DefaultValue": {
                                "varName": "John",
                                "varAge": 25,
                                "otherKey": "value",
                                "varCity": "Beijing"
                            }
                        }
                    ]
                }
                """;
            
            Console.WriteLine("Original JSON Arguments:");
            Console.WriteLine(jsonArguments);
            Console.WriteLine();
            
            // Parse JSON to simulate SemanticKernel deserialization
            using var jsonDoc = JsonDocument.Parse(jsonArguments);
            var root = jsonDoc.RootElement;
            
            var expression = root.GetProperty("expression").GetString() ?? string.Empty;
            var variablesElement = root.GetProperty("variables");
            
            // Deserialize variables (simulating how SemanticKernel would deserialize)
            var variables = new List<Plugins.VariableClassWithObjectValue>();
            foreach (var varElement in variablesElement.EnumerateArray())
            {
                var varName = varElement.GetProperty("VarName").GetString() ?? string.Empty;
                var varTypeStr = varElement.GetProperty("VarType").GetString() ?? "String";
                var varType = Enum.Parse<VariableType>(varTypeStr, ignoreCase: true);
                
                // Get DefaultValue as JsonElement (this is how SemanticKernel would pass it)
                var defaultValueElement = varElement.GetProperty("DefaultValue");
                
                var variable = new Plugins.VariableClassWithObjectValue
                {
                    VarName = varName,
                    VarType = varType,
                    DefaultValue = defaultValueElement // JsonElement
                };
                
                variables.Add(variable);
            }
            
            Console.WriteLine("Deserialized Variables:");
            foreach (var variable in variables)
            {
                Console.WriteLine($"  VarName: {variable.VarName}");
                Console.WriteLine($"  VarType: {variable.VarType}");
                Console.WriteLine($"  DefaultValue Type: {variable.DefaultValue?.GetType().Name ?? "null"}");
                if (variable.DefaultValue is JsonElement jsonElement)
                {
                    Console.WriteLine($"  DefaultValue (JsonElement): {jsonElement.GetRawText()}");
                }
                else
                {
                    Console.WriteLine($"  DefaultValue: {variable.DefaultValue}");
                }
            }
            Console.WriteLine();
            
            // Convert to VariableClass
            Console.WriteLine("Converting to VariableClass:");
            var convertedVariables = variables.Select(v => v.ToVariableClass()).ToList();
            foreach (var variable in convertedVariables)
            {
                Console.WriteLine($"  VarName: {variable.VarName}");
                Console.WriteLine($"  VarType: {variable.VarType}");
                Console.WriteLine($"  DefaultValue (string): {variable.DefaultValue}");
                
                // Test GetDefaultValue
                var deserializedValue = variable.GetDefaultValue();
                Console.WriteLine($"  Deserialized Value Type: {deserializedValue?.GetType().Name ?? "null"}");
                if (deserializedValue is Dictionary<string, object> dict)
                {
                    Console.WriteLine($"  Deserialized Value: {string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }
                else
                {
                    Console.WriteLine($"  Deserialized Value: {deserializedValue}");
                }
            }
            Console.WriteLine();
            
            // Test with ExpressionAgentPlugin
            Console.WriteLine("Testing with ExpressionAgentPlugin:");
            var serviceProvider = new ServiceCollection()
                .AddServerServices()
                .BuildServiceProvider();
            
            // Create a mock IToolHandlerProvider
            var toolHandler = serviceProvider.GetRequiredService<IExpressionAgentToolHandler>();
            var toolHandlerProvider = new MockToolHandlerProvider(toolHandler);
            
            var plugin = new Plugins.ExpressionAgentPlugin(toolHandlerProvider);
            
            // Create the dict variable first
            var dictVar = new VariableClass
            {
                VarName = "dict",
                VarType = VariableType.Dictionary
            };
            dictVar.SetDefaultValue(new Dictionary<string, object>());
            
            // Set variable
            toolHandler.SetVariable(dictVar);
            
            // Convert variables to List<object> (SemanticKernel will pass List<object>)
            // Each variable will be passed as an object (could be JsonElement, Dictionary, etc.)
            List<object>? variablesList = null;
            if (variables != null && variables.Count > 0)
            {
                variablesList = new List<object>();
                foreach (var variable in variables)
                {
                    // Convert VariableClassWithObjectValue to JsonElement (simulating SemanticKernel behavior)
                    var jsonString = JsonSerializer.Serialize(new
                    {
                        variable.VarName,
                        VarType = variable.VarType.ToString(),
                        variable.DefaultValue
                    });
                    using var varJsonDoc = JsonDocument.Parse(jsonString);
                    variablesList.Add(varJsonDoc.RootElement.Clone()); // Clone to avoid disposal issues
                }
            }
            
            // Now test TestExpression
            var result = await plugin.TestExpressionAsync(expression, variablesList);
            
            Console.WriteLine("TestExpression Result:");
            Console.WriteLine(result);
            
            Console.WriteLine("\n=== Test Completed ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in plugin parameter conversion test");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Mock IToolHandlerProvider for testing
    /// </summary>
    private class MockToolHandlerProvider : Services.IToolHandlerProvider
    {
        private readonly IExpressionAgentToolHandler _toolHandler;
        
        public MockToolHandlerProvider(IExpressionAgentToolHandler toolHandler)
        {
            _toolHandler = toolHandler;
        }
        
        public IExpressionAgentToolHandler ToolHandler => _toolHandler;
    }

    /// <summary>
    /// Create or get Quicker Code Editor handler
    /// Prompts user for window handle or uses default
    /// </summary>
    private static async Task<QuickerCodeEditorToolHandler?> CreateOrGetQuickerCodeEditorHandlerAsync(
        QuickerServerClientConnector connector,
        ILogger logger)
    {
        try
        {
            // Wait for connection to be established
            var connected = await connector.WaitConnectAsync(TimeSpan.FromSeconds(10));

            if (!connected)
            {
                logger.LogWarning("Quicker service not connected, cannot create Code Editor handler");
                Console.WriteLine("Warning: Quicker service is not connected. Please ensure the Quicker application is running.");
                return null;
            }

            // Use GetOrCreateCodeEditorAsync to get or create Code Editor
            try
            {
                Console.WriteLine("\n=== Quicker Code Editor Handler Setup ===");
                Console.WriteLine("Getting or creating Code Editor window...");
                
                var handlerId = await connector.ServiceClient.GetOrCreateCodeEditorAsync();
                
                if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                {
                    logger.LogWarning("Failed to get or create Code Editor, got handler ID: {HandlerId}", handlerId);
                    Console.WriteLine("Warning: Failed to get or create Code Editor window.");
                    return null;
                }

                // Create handler using handlerId
                var handler = new QuickerCodeEditorToolHandler(handlerId, connector);
                logger.LogInformation("Successfully created Quicker Code Editor handler with handler ID: {HandlerId}", handlerId);
                Console.WriteLine($"✓ Successfully connected to Code Editor window (Handler ID: {handlerId})");
                return handler;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get or create Code Editor handler");
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CreateOrGetQuickerCodeEditorHandlerAsync");
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
}

