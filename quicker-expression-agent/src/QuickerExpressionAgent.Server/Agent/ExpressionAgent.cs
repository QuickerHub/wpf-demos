using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;
using System.Collections;
using System.Linq;
using System.Text.Json;

namespace QuickerExpressionAgent.Server.Agent;

/// <summary>
/// Expression Agent using Semantic Kernel's official ChatCompletionAgent framework
/// </summary>
public partial class ExpressionAgent : IToolHandlerProvider
{
    private readonly Kernel _kernel;
    private readonly ChatCompletionAgent _agent;

    /// <summary>
    /// Expression executor for executing expressions (can be modified at runtime)
    /// </summary>
    public IExpressionExecutor Executor { get; set; }

    /// <summary>
    /// Gets the current tool handler (can be modified at runtime)
    /// </summary>
    public IExpressionAgentToolHandler ToolHandler { get; set; }

    // Current state
    private string? _currentExpression;
    private List<VariableClass> _variables = new();

    // Persistent chat history for conversation memory
    private readonly ChatHistory _chatHistory = new();


    /// <summary>
    /// Callback for reporting agent progress
    /// </summary>
    public delegate void AgentProgressCallback(AgentStep step, string content);

    /// <summary>
    /// Callback for streaming content updates (for real-time UI updates)
    /// </summary>
    public delegate void AgentStreamingCallback(AgentStepType stepType, string partialContent, bool isComplete);

    /// <summary>
    /// Types of agent steps
    /// </summary>
    public enum AgentStepType
    {
        Thought,
        ToolCall,
        FinalAnswer
    }

    /// <summary>
    /// A single agent step
    /// </summary>
    public class AgentStep
    {
        public AgentStepType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ToolName { get; set; }
        public Dictionary<string, string>? ToolArguments { get; set; }
        public string? ToolCallId { get; set; }
    }

    /// <summary>
    /// Base class for stream items
    /// </summary>
    public abstract partial class StreamItem : ObservableObject
    {
        [ObservableProperty]
        private bool _isComplete;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;
    }

    /// <summary>
    /// Stream item for regular text content
    /// </summary>
    public partial class ContentStreamItem : StreamItem
    {
        [ObservableProperty]
        private string _text = string.Empty;
    }

    /// <summary>
    /// Stream item for function/tool call
    /// </summary>
    public partial class FunctionCallStreamItem : StreamItem
    {
        [ObservableProperty]
        private string _functionName = string.Empty;

        [ObservableProperty]
        private string _arguments = string.Empty;

        [ObservableProperty]
        private string _functionCallId = string.Empty;

        [ObservableProperty]
        private string? _result;
    }


    public ExpressionAgent(Kernel kernel, IExpressionExecutor executor, IExpressionAgentToolHandler? toolHandler = null)
    {
        _kernel = kernel;
        Executor = executor;
        ToolHandler = toolHandler!;

        // Add plugin to kernel (plugin uses IToolHandlerProvider, so it can be added even if toolHandler is null)
        // Plugin will access toolHandler via provider.ToolHandler at runtime
        var plugin = new ExpressionAgentPlugin(this);
        _kernel.Plugins.AddFromObject(plugin, "ExpressionAgent");

        // Create ChatCompletionAgent with system instructions
        // Tools are automatically available from kernel plugins
        var systemInstructions = BuildSystemInstructions();
        _agent = new ChatCompletionAgent
        {
            Kernel = _kernel,
            Instructions = systemInstructions,
            Name = "ExpressionAgent"
        };
    }

    /// <summary>
    /// Generate expression using Semantic Kernel's ChatCompletionAgent
    /// </summary>
    public async Task GenerateExpressionAsync(
        string naturalLanguage,
        AgentProgressCallback? progressCallback = null,
        AgentStreamingCallback? streamingCallback = null,
        CancellationToken cancellationToken = default)
    {
        _currentExpression = null;

        // Build initial context with variables (Agent can get them via GetExternalVariables tool)
        var contextMessage = BuildContextMessage();

        progressCallback?.Invoke(
            new AgentStep { Type = AgentStepType.Thought, Content = "Starting agent..." },
            "开始处理用户请求");

        // Initialize chat history on first use (add system instructions once)
        if (_chatHistory.Count == 0 && !string.IsNullOrEmpty(contextMessage))
        {
            _chatHistory.AddSystemMessage(contextMessage);
        }

        // Add user request to persistent chat history
        _chatHistory.AddUserMessage(naturalLanguage);

        // Get chat completion service from kernel
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Configure execution settings to auto-invoke kernel functions
        // This will automatically execute tool calls and add results to chat history
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        try
        {
            // Use streaming API for real-time updates
            string accumulatedContent = string.Empty;

            // Stream response from chat completion service
            await foreach (var streamingContent in chatCompletion.GetStreamingChatMessageContentsAsync(
                _chatHistory,
                executionSettings: executionSettings,
                _kernel,
                cancellationToken))
            {
                // Real-time streaming output - accumulate content
                if (!string.IsNullOrEmpty(streamingContent.Content))
                {
                    accumulatedContent = streamingContent.Content;

                    // Send streaming updates to UI
                    if (streamingCallback != null)
                    {
                        streamingCallback(AgentStepType.Thought, accumulatedContent, false);
                    }
                }
            }

            // Send final content update
            if (streamingCallback != null && !string.IsNullOrEmpty(accumulatedContent))
            {
                streamingCallback(AgentStepType.Thought, accumulatedContent, true);
            }

            // Send progress callback with final content
            if (progressCallback != null && !string.IsNullOrWhiteSpace(accumulatedContent))
            {
                progressCallback?.Invoke(
                    new AgentStep
                    {
                        Type = AgentStepType.Thought,
                        Content = accumulatedContent
                    },
                    accumulatedContent);
            }
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke(
                new AgentStep { Type = AgentStepType.Thought, Content = $"Error: {ex.Message}" },
                $"发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate expression and return stream of items (content or function calls)
    /// Each item can be updated over time as streaming progresses
    /// </summary>
    public async IAsyncEnumerable<StreamItem> GenerateExpressionAsStreamAsync(
        string naturalLanguage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _currentExpression = null;

        // Build initial context with variables (Agent can get them via GetExternalVariables tool)
        var contextMessage = BuildContextMessage();

        // Initialize chat history on first use (add system instructions once)
        if (_chatHistory.Count == 0 && !string.IsNullOrEmpty(contextMessage))
        {
            _chatHistory.AddSystemMessage(contextMessage);
        }

        // Add user request to persistent chat history
        _chatHistory.AddUserMessage(naturalLanguage);

        // Get chat completion service from kernel
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Configure execution settings to auto-invoke kernel functions
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        // Track the last processed message count to detect new tool call results
        int lastMessageCount = _chatHistory.Count;

        // Track current content item for incremental updates
        ContentStreamItem? currentContentItem = null;

        // Track function call items using queue (FIFO for sequential matching)
        Queue<FunctionCallStreamItem> functionCallQueue = new();

        // Stream response from chat completion service
        // Note: Cannot use try-catch with yield return, so exceptions will propagate to caller
        await foreach (var chatUpdate in chatCompletion.GetStreamingChatMessageContentsAsync(
            _chatHistory,
            executionSettings: executionSettings,
            _kernel,
            cancellationToken))
        {

            // Check for new messages in chat history (likely tool call results)
            if (_chatHistory.Count > lastMessageCount)
            {
                for (int i = lastMessageCount; i < _chatHistory.Count; i++)
                {
                    var newMessage = _chatHistory[i];
                    if (newMessage.Role == AuthorRole.Tool)
                    {
                        // Get the first incomplete function call item from queue (FIFO)
                        FunctionCallStreamItem? functionCallItem = functionCallQueue
                            .FirstOrDefault(fc => !fc.IsComplete && string.IsNullOrEmpty(fc.Result));

                        if (functionCallItem != null)
                        {
                            // Update function call item with result (no yield, item already exists)
                            functionCallItem.Result = newMessage.Content;
                            functionCallItem.IsComplete = true;
                            functionCallItem.Timestamp = DateTime.Now;

                            // Remove from queue (rebuild queue without the completed item)
                            var tempQueue = new Queue<FunctionCallStreamItem>();
                            while (functionCallQueue.Count > 0)
                            {
                                var item = functionCallQueue.Dequeue();
                                if (item != functionCallItem)
                                {
                                    tempQueue.Enqueue(item);
                                }
                            }
                            functionCallQueue = tempQueue;
                        }
                    }
                }
                lastMessageCount = _chatHistory.Count;
            }

            // Process regular content updates
            if (!string.IsNullOrEmpty(chatUpdate.Content))
            {
                if (currentContentItem == null)
                {
                    // Create new content item
                    currentContentItem = new ContentStreamItem
                    {
                        Text = chatUpdate.Content,
                        IsComplete = false,
                        Timestamp = DateTime.Now
                    };
                    yield return currentContentItem; // Yield only when created
                }
                else
                {
                    // Update existing content item incrementally (real-time update, no yield)
                    currentContentItem.Text += chatUpdate.Content;
                    currentContentItem.Timestamp = DateTime.Now;
                }
            }

            // Process function call updates
            var functionCallUpdates = chatUpdate.Items.OfType<StreamingFunctionCallUpdateContent>();
            foreach (var update in functionCallUpdates)
            {
                currentContentItem = null; // Reset current content item when processing function call
                // Get the last incomplete function call item in queue (for updating arguments)
                FunctionCallStreamItem functionCallItem;
                if (!string.IsNullOrEmpty(update.CallId) || !string.IsNullOrEmpty(update.Name))
                {
                    // Try to find by CallId first
                    functionCallItem = new FunctionCallStreamItem()
                    {
                        FunctionName = update.Name!,
                        FunctionCallId = update.CallId!,
                    };
                    functionCallQueue.Enqueue(functionCallItem);
                    yield return functionCallItem;
                }
                else
                {
                    functionCallItem = functionCallQueue.LastOrDefault() ?? throw new InvalidOperationException("No function call item found to update.");
                    if (!string.IsNullOrEmpty(update.Arguments))
                    {
                        functionCallItem.Arguments += update.Arguments;
                        functionCallItem.Timestamp = DateTime.Now;
                    }
                }
            }
        }

        // Mark current content item as complete if exists (no yield, item already exists)
        if (currentContentItem != null)
        {
            currentContentItem.IsComplete = true;
            currentContentItem.Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Generate expression with direct console output (no callbacks, prints directly to console)
    /// </summary>
    public async Task GenerateExpressionWithConsoleOutputAsync(
        string naturalLanguage,
        CancellationToken cancellationToken = default)
    {
        _currentExpression = null;

        // Build initial context with variables (Agent can get them via GetExternalVariables tool)
        var contextMessage = BuildContextMessage();

        // Initialize chat history on first use (add system instructions once)
        if (_chatHistory.Count == 0)
        {
            var systemInstructions = BuildSystemInstructions();
            if (!string.IsNullOrEmpty(systemInstructions))
            {
                _chatHistory.AddSystemMessage(systemInstructions);
            }
            if (!string.IsNullOrEmpty(contextMessage))
            {
                _chatHistory.AddSystemMessage(contextMessage);
            }
        }

        // Add user request to persistent chat history
        _chatHistory.AddUserMessage(naturalLanguage);

        // Get chat completion service from kernel
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Configure execution settings to auto-invoke kernel functions
        // This will automatically execute tool calls and add results to chat history
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        try
        {
            // Track the last processed message count to detect new tool call results in real-time
            int lastMessageCount = _chatHistory.Count;

            // Stream response from chat completion service
            await foreach (var chatUpdate in chatCompletion.GetStreamingChatMessageContentsAsync(
                _chatHistory,
                executionSettings: executionSettings,
                _kernel,
                cancellationToken))
            {
                // Check for tool call updates in streaming content
                var functionCallUpdates = chatUpdate.Items.OfType<StreamingFunctionCallUpdateContent>();
                foreach (var update in functionCallUpdates)
                {
                    // Print tool call start when name is available
                    if (!string.IsNullOrEmpty(update.Name))
                    {
                        Console.WriteLine($"\n[Tool Call] {update.Name}");
                        if (!string.IsNullOrEmpty(update.Arguments))
                        {
                            Console.Write("[Arguments] ");
                        }
                    }

                    // Print arguments incrementally (streaming updates)
                    if (!string.IsNullOrEmpty(update.Arguments))
                    {
                        Console.Write(update.Arguments);
                    }
                }

                // Check for new messages in chat history (likely tool call results)
                if (_chatHistory.Count > lastMessageCount)
                {
                    // New message(s) added to chat history (likely tool call results)
                    Console.WriteLine();
                    for (int i = lastMessageCount; i < _chatHistory.Count; i++)
                    {
                        var newMessage = _chatHistory[i];
                        if (newMessage.Role == AuthorRole.Tool)
                        {
                            Console.WriteLine($"[Tool Call Result] {newMessage.Content}");
                        }
                    }
                    lastMessageCount = _chatHistory.Count;
                }

                // Real-time streaming output - print incremental content
                if (!string.IsNullOrEmpty(chatUpdate.Content))
                {
                    Console.Write(chatUpdate.Content);
                }

                // Print role information if available
                if (chatUpdate.Role is not null)
                {
                    Console.WriteLine();
                    Console.Write($"[{chatUpdate.Role}] ");
                }
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Build system instructions for the agent
    /// Tools are automatically provided by the framework, no need to describe them
    /// </summary>
    private string BuildSystemInstructions()
    {
        return """
            You are an AI agent that generates C# expressions for Quicker software.
            
            ## Your Workflow:
            Your core function is to **call tools to create or modify expressions**. The workflow is:
            1. **Get external variables** - First, retrieve the current external variables (if any) using GetExternalVariables tool
            2. **Analyze user intent** - Understand whether the user wants to:
               - **Create a new expression** from scratch
               - **Modify the existing expression** based on user's request
            3. **Get current expression** (if modifying) - If the user wants to modify the existing expression, call GetCurrentExpressionDescription tool to retrieve the current expression and variables
            4. **Determine input variables** - Based on the user's requirement and existing expression, determine what external variables are needed
            5. **Create or modify expression** - Use ModifyExpression tool to generate or modify the expression
            6. **Test the expression** - Use TestExpression tool with the variables to verify it works
            7. **Carefully check test results** - After testing, carefully examine the test result to ensure it matches the user's requirements:
               - Check if the result value is correct and matches the expected output
               - Verify that the expression logic correctly implements the user's intent
               - If the result doesn't match expectations, analyze why and fix the expression
            8. **Fix errors** - If the test fails or the result doesn't match expectations, modify the expression or adjust variables, then repeat step 6-7
            9. **Output final result** - Once the expression executes successfully and produces the expected result, call SetExpression with the final working expression. Variables should be created/updated separately using CreateVariable method before calling SetExpression.
            
            ## Expression Model (Function Analogy):
            Think of an expression as a function:
            - **{variableName}** is like a function parameter: `function(var variableName)` - it's an INPUT parameter
            - **The expression body** is like the function body - it contains the computation logic
            - **The expression result** is like the function return value - it's what the expression evaluates to
            
            ## Expression Format:
            - Expression is **pure C# code** - standard C# syntax that can be executed directly
            - **CRITICAL: Use {variableName} format to get input variables** - This is the ONLY way to reference external variables (input variables) in expressions
            - The **{variableName}** syntax is a Quicker-specific format for referencing external variables that provide input to the expression
            - **ALL external variables MUST be referenced using {variableName} format** - e.g., {userName}, {age}, {items}
            - During execution, {variableName} will be replaced with the actual variable name (varname) for parsing
            - The expression itself remains valid C# code and can execute normally after replacement
            - Expression can be multi-line
            - Prefer LINQ expressions over verbose loops
            - Write concise code
            
            ## .NET Framework Version:
            - Expressions are executed in **.NET Framework 4.7.2** environment
            - **IMPORTANT: Random number generation** - Be aware that `new Random()` creates a new instance with a time-based seed:
              * If you create multiple `Random` instances quickly (e.g., in a loop), they may use the same seed and produce identical sequences
              * Example problem: `Enumerable.Range(0, 10).Select(i => new Random().Next())` may produce the same number for all iterations
              * Solution: Create a single `Random` instance and reuse it, or use a static instance: `var rnd = new Random(); Enumerable.Range(0, 10).Select(i => rnd.Next())`
            - Some .NET Framework 4.7.2 APIs may have different behavior or limitations compared to newer .NET versions
            - Always test expressions to ensure they work correctly in this environment
            
            ## Variable Reference Rules ({variableName} as Function Parameters):
            - **{variableName} is like a function parameter - it's an INPUT, not a variable you can assign to**
            - **You CANNOT assign values to {variableName} directly. For example, {varname} = value is NOT allowed and will NOT work.**
            - **Direct assignment to {variableName} is useless** - it's like trying to assign to a function parameter, which doesn't affect the original variable
            - **Exception: For reference types (like Dictionary, List, Object), you CAN modify properties/members** - e.g., `{dict}["key"] = value` or `{list}.Add(item)` will work because you're modifying the object's properties, not reassigning the parameter
            - If you need to use a variable named "userName", write it as **{userName}** in the expression
            - Example expression: `"Hello, " + {userName} + "!"`
            - During execution, {userName} will be replaced with userName, making it valid C#: `"Hello, " + userName + "!"`
            - **DO NOT** declare variables that are external inputs - use {variableName} format instead
            - **DO NOT** use variable names directly without curly braces for external variables
            
            ## Expression Return Value (Function Body):
            - **The expression is like a function body** - it computes and returns a result
            - **The expression result is automatically returned** - you don't need to explicitly return it
            - **DO NOT write assignment statements like {outputVar} = {inputVar}.Where(...).ToDictionary(...)**
            - **INSTEAD, write the computation directly: {inputVar}.Where(...).ToDictionary(...)**
            - The result of the expression will be automatically returned and used by Quicker
            - **An expression can return a value (like a function)** or return void (like an action)
            - Example CORRECT (returns value): `{inputDictionary}.Where(kv => kv.Key.StartsWith("var")).ToDictionary(kv => kv.Key, kv => kv.Value)`
            - Example CORRECT (void/action): `{list}.Clear()` or `{dict}.Remove("key")`
            - Example WRONG: `{outputDictionary} = {inputDictionary}.Where(...).ToDictionary(...)` - This will NOT work! Direct assignment to {variableName} is useless.
            
            ## Variable Types:
            Supported variable types are STRICTLY limited to: String, Int, Double, Bool, DateTime, ListString, Dictionary, Object
            
            ## Variable Naming Convention:
            When creating new variables, use **concise, short names**:
            - Keep variable names short and simple (e.g., `text`, `list`, `dict`, `num`, `flag`, `date`)
            - Use numbered suffixes when creating multiple variables of the same type (e.g., `text1`, `text2`, `list1`, `list2`)
            - Prefer type-based abbreviations or short descriptive names
            - Examples: `text`, `list1`, `dict`, `num`, `flag`, `date`, `obj`
            
            ## Important:
            - Always test expressions before calling SetExpression
            - Only call SetExpression when the expression has been tested and works correctly
            - Use TestExpression tool to verify your expression works with the variables
            - **Carefully check test results** - After each TestExpression call, examine the result to ensure:
              * The result value matches the user's requirements and expected output
              * The expression logic correctly implements what the user asked for
              * If the result is unexpected, analyze the issue and fix the expression before proceeding
              * Do not proceed to SetExpression if the test result doesn't match the user's intent
            """;
    }

    /// <summary>
    /// Build context message with existing variables information
    /// </summary>
    private string BuildContextMessage()
    {
        if (_variables == null || !_variables.Any())
        {
            return string.Empty;
        }

        var varList = string.Join("\n", _variables.Select(v =>
            $"- {v.VarName} ({v.VarType}): {v.GetDefaultValue()}"));

        return $"Existing variables (do not recreate these):\n{varList}";
    }

    /// <summary>
    /// Get tool call identifier from ChatMessageContent (tool result)
    /// Look for FunctionCallContent in Items collection to get the function call ID
    /// </summary>
    private string? GetToolCallIdFromMessage(ChatMessageContent message)
    {
        // Check Items collection for FunctionCallContent
        if (message.Items != null)
        {
            foreach (var item in message.Items)
            {
                // Look for FunctionCallContent which contains the function call ID
                if (item is FunctionCallContent functionCall)
                {
                    return functionCall.Id;
                }
            }
        }
        return null;
    }
}
