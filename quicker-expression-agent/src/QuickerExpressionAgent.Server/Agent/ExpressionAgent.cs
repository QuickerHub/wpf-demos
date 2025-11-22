using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;
using SharpToken;
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

    // Token encoder for precise token counting
    private readonly GptEncoding _tokenEncoder;


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

        // Initialize token encoder (most modern models use cl100k_base encoding)
        // This includes: gpt-4, gpt-3.5-turbo, gpt-4-turbo, and most OpenAI-compatible models
        _tokenEncoder = GptEncoding.GetEncoding("cl100k_base");

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
    /// Prepare chat history and execution settings for agent execution
    /// </summary>
    private (IChatCompletionService chatCompletion, OpenAIPromptExecutionSettings executionSettings) PrepareChatHistory(
        string naturalLanguage,
        ChatHistory chatHistory,
        bool includeSystemInstructions = false)
    {
        _currentExpression = null;

        // Build initial context with variables (Agent can get them via GetExternalVariables tool)
        var contextMessage = BuildContextMessage();

        // Initialize chat history on first use
        if (chatHistory.Count == 0)
        {
            if (includeSystemInstructions)
            {
                var systemInstructions = BuildSystemInstructions();
                if (!string.IsNullOrEmpty(systemInstructions))
                {
                    chatHistory.AddSystemMessage(systemInstructions);
                }
            }
            if (!string.IsNullOrEmpty(contextMessage))
            {
                chatHistory.AddSystemMessage(contextMessage);
            }
        }

        // Add user request to chat history
        chatHistory.AddUserMessage(naturalLanguage);

        // Get chat completion service from kernel
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Configure execution settings to auto-invoke kernel functions
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        return (chatCompletion, executionSettings);
    }

    /// <summary>
    /// Generate expression using Semantic Kernel's ChatCompletionAgent
    /// </summary>
    public async Task GenerateExpressionAsync(
        string naturalLanguage,
        ChatHistory chatHistory,
        AgentProgressCallback? progressCallback = null,
        AgentStreamingCallback? streamingCallback = null,
        CancellationToken cancellationToken = default)
    {
        progressCallback?.Invoke(
            new AgentStep { Type = AgentStepType.Thought, Content = "Starting agent..." },
            "开始处理用户请求");

        var (chatCompletion, executionSettings) = PrepareChatHistory(naturalLanguage, chatHistory);

        try
        {
            // Use streaming API for real-time updates
            string accumulatedContent = string.Empty;

            // Stream response from chat completion service
            await foreach (var streamingContent in chatCompletion.GetStreamingChatMessageContentsAsync(
                chatHistory,
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
        ChatHistory chatHistory,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (chatCompletion, executionSettings) = PrepareChatHistory(naturalLanguage, chatHistory);

        // Track the last processed message count to detect new tool call results
        int lastMessageCount = chatHistory.Count;

        // Track current content item for incremental updates
        ContentStreamItem? currentContentItem = null;

        // Track function call items using queue (FIFO for sequential matching)
        Queue<FunctionCallStreamItem> functionCallQueue = new();

        // Stream response from chat completion service
        // Note: Cannot use try-catch with yield return, so exceptions will propagate to caller
        await foreach (var chatUpdate in chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings: executionSettings,
            _kernel,
            cancellationToken))
        {

            // Check for new messages in chat history (likely tool call results)
            if (chatHistory.Count > lastMessageCount)
            {
                for (int i = lastMessageCount; i < chatHistory.Count; i++)
                {
                    var newMessage = chatHistory[i];
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
                lastMessageCount = chatHistory.Count;
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
        ChatHistory chatHistory,
        CancellationToken cancellationToken = default)
    {
        var (chatCompletion, executionSettings) = PrepareChatHistory(naturalLanguage, chatHistory, includeSystemInstructions: true);

        try
        {
            // Track the last processed message count to detect new tool call results in real-time
            int lastMessageCount = chatHistory.Count;

            // Stream response from chat completion service
            await foreach (var chatUpdate in chatCompletion.GetStreamingChatMessageContentsAsync(
                chatHistory,
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
                if (chatHistory.Count > lastMessageCount)
                {
                    // New message(s) added to chat history (likely tool call results)
                    Console.WriteLine();
                    for (int i = lastMessageCount; i < chatHistory.Count; i++)
                    {
                        var newMessage = chatHistory[i];
                        if (newMessage.Role == AuthorRole.Tool)
                        {
                            Console.WriteLine($"[Tool Call Result] {newMessage.Content}");
                        }
                    }
                    lastMessageCount = chatHistory.Count;
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
        return $$"""
            You are an AI agent that generates C# expressions for Quicker software.
            
            ## Your Workflow:
            Your core function is to **call tools to create or modify expressions**. The workflow is:
            1. **Get current state** - Call {{nameof(ExpressionAgentPlugin.GetCurrentExpressionDescription)}} tool to retrieve both the current expression and all external variables in one call. This tool returns a formatted description containing the expression code and variable list with their types.
            2. **Analyze user intent** - Understand whether the user wants to:
               - **Create a new expression** from scratch
               - **Modify the existing expression** based on user's request
            3. **Evaluate current expression** - **IMPORTANT**: Before making any changes, carefully evaluate whether the current expression already meets the user's requirements:
               - If the current expression **already correctly implements** the user's requested functionality, you should:
                 * **Do nothing** - Inform the user that the current expression already meets their needs
                 * **OR suggest optimizations** - If you see potential improvements (performance, readability, maintainability), present them to the user and let them decide whether to apply the changes
               - Only proceed to modify the expression if it **does not** meet the user's requirements or if the user explicitly requests changes
            4. **Determine input variables** - Based on the user's requirement and existing expression (from step 1), determine what external variables are needed. Create or update variables using {{nameof(ExpressionAgentPlugin.CreateVariable)}} method as needed.
            5. **Generate or modify expression** - Based on the user's requirements, generate the new expression code or modify the existing one. You can write the expression code directly.
            6. **Test the expression** - Use {{nameof(ExpressionAgentPlugin.TestExpressionAsync)}} tool to verify it works (see tool description for best practices on variable default values). **IMPORTANT: When the test succeeds, the expression is automatically set. You don't need to call {{nameof(ExpressionAgentPlugin.SetExpression)}} separately.**
            7. **Fix errors** - If the test fails or the result doesn't match expectations, adjust the expression code or variables, then repeat step 6
            8. **Handle persistent failures** - **CRITICAL**: If you have tried multiple times (3+ attempts) and the expression still fails to execute correctly:
               - **Stop and think** - Consider that the problem might not be with your expression logic, but with the execution environment or constraints
               - **Possible environment issues** to consider:
                 * Missing or unavailable namespaces/types in the execution environment
                 * API differences between .NET Framework 4.7.2 and newer versions
                 * Runtime limitations or restrictions in the Quicker execution context
                 * Variable type conversion issues that cannot be resolved programmatically
                 * Expression format constraints that prevent certain code patterns
               - **Output your analysis** - Clearly explain to the user:
                 * What you've tried and why it should work logically
                 * What specific error or unexpected behavior you're encountering
                 * Your hypothesis about what might be causing the issue (environment, API differences, etc.)
                 * Suggest that the user might need to check the execution environment, verify available APIs, or consider alternative approaches
               - **Don't keep trying the same approach** - If multiple attempts with different variations all fail, it's likely an environmental or fundamental constraint issue
            9. **Output final result** - Once the expression executes successfully and produces the expected result, the expression is automatically set by {{nameof(ExpressionAgentPlugin.TestExpressionAsync)}}. Variables should already be created/updated using {{nameof(ExpressionAgentPlugin.CreateVariable)}} method. **No need to call {{nameof(ExpressionAgentPlugin.SetExpression)}} separately.**
            
            ## Expression Format Reference:
            **IMPORTANT: For detailed expression format specifications, including {variableName} syntax, registered namespaces, variable reference rules, and examples, please refer to the {{nameof(ExpressionAgentPlugin.TestExpressionAsync)}} tool description.** The tool descriptions contain comprehensive information about expression format that you should follow.
            
            ## .NET Framework Version:
            - Expressions are executed in **.NET Framework 4.7.2** environment
            - **IMPORTANT: Random number generation** - `new Random()` uses time-based seed. Creating multiple instances quickly may produce identical sequences. **Recommended: Reuse a single Random instance** - `var rnd = new Random(); var numbers = new List<int>(); for (int i = 0; i < 10; i++) numbers.Add(rnd.Next());`
            - Some APIs may have different behavior compared to newer .NET versions - always test expressions
            
            ## Variable Types:
            Supported variable types are STRICTLY limited to: String, Int, Double, Bool, DateTime, ListString, Dictionary, Object
            
            ## Important:
            - Always test expressions with {{nameof(ExpressionAgentPlugin.TestExpressionAsync)}} to verify they work correctly (see tool description for details)
            - When {{nameof(ExpressionAgentPlugin.TestExpressionAsync)}} succeeds, the expression is automatically set. You typically don't need to call {{nameof(ExpressionAgentPlugin.SetExpression)}} separately
            - Only use {{nameof(ExpressionAgentPlugin.SetExpression)}} if you need to set an expression without testing it first (rare case)
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

    /// <summary>
    /// Calculate precise token count for the chat history using SharpToken
    /// </summary>
    /// <param name="chatHistory">Chat history to count tokens for</param>
    /// <returns>Precise token count</returns>
    public int EstimateTokenCount(ChatHistory chatHistory)
    {
        int totalTokens = 0;
        
        foreach (var message in chatHistory)
        {
            // Count tokens in message content
            if (!string.IsNullOrEmpty(message.Content))
            {
                var tokens = _tokenEncoder.Encode(message.Content);
                totalTokens += tokens.Count;
            }
            
            // Count tokens in tool call items (if any)
            if (message.Items != null)
            {
                foreach (var item in message.Items)
                {
                    if (item is ChatMessageContent itemContent && !string.IsNullOrEmpty(itemContent.Content))
                    {
                        var tokens = _tokenEncoder.Encode(itemContent.Content);
                        totalTokens += tokens.Count;
                    }
                }
            }
        }
        
        return totalTokens;
    }

    /// <summary>
    /// Get chat history message count
    /// </summary>
    /// <param name="chatHistory">Chat history to get count for</param>
    /// <returns>Number of messages in chat history</returns>
    public int GetChatHistoryCount(ChatHistory chatHistory)
    {
        return chatHistory.Count;
    }
}
