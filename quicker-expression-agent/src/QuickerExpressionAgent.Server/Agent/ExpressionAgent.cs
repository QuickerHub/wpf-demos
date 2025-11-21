using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;
using System.Text.Json;

namespace QuickerExpressionAgent.Server.Agent;

/// <summary>
/// Expression Agent using Semantic Kernel's official ChatCompletionAgent framework
/// </summary>
public class ExpressionAgent : IToolHandlerProvider
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
    }

    public ExpressionAgent(Kernel kernel, IExpressionExecutor executor, IExpressionAgentToolHandler? toolHandler = null)
    {
        _kernel = kernel;
        Executor = executor;
        ToolHandler = toolHandler!;
        
        // Add plugin to kernel if tool handler is available
        // Pass this as IToolHandlerProvider so plugin can access the tool handler
        if (toolHandler != null)
        {
            var plugin = new ExpressionAgentPlugin(this);
            _kernel.Plugins.AddFromObject(plugin, "ExpressionAgent");
        }
        
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
                            Console.Write($"[Tool Call Result] {newMessage.Content}");
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
                
                // Check for tool call updates in streaming content
                var functionCallUpdates = chatUpdate.Items.OfType<StreamingFunctionCallUpdateContent>();
                foreach (var update in functionCallUpdates)
                {
                    if (!string.IsNullOrEmpty(update.Name))
                    {
                        Console.WriteLine($"\n[Tool Call Start] {update.Name}");
                        Console.Write("[Tool Call Arguments] ");
                    }
                    if (!string.IsNullOrEmpty(update.Arguments))
                    {
                        Console.Write(update.Arguments); // Incremental arguments printing
                    }
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
            7. **Fix errors** - If the test fails, modify the expression or adjust variables, then repeat step 6
            8. **Output final result** - Once the expression executes successfully, call SetExpression with the final working expression. Variables should be created/updated separately using CreateVariable method before calling SetExpression.
            
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
            
            ## Variable Reference Rules:
            - **To get input from external variables, you MUST use {variableName} format**
            - **IMPORTANT: You CANNOT assign values to {variableName} variables. For example, {varname} = value is NOT allowed.**
            - If you need to use a variable named "userName", write it as **{userName}** in the expression
            - Example expression: `"Hello, " + {userName} + "!"`
            - During execution, {userName} will be replaced with userName, making it valid C#: `"Hello, " + userName + "!"`
            - **DO NOT** declare variables that are external inputs - use {variableName} format instead
            - **DO NOT** use variable names directly without curly braces for external variables
            
            ## Variable Types:
            Supported variable types are STRICTLY limited to: String, Int, Double, Bool, DateTime, ListString, Dictionary, Object
            
            ## Important:
            - Always test expressions before calling SetExpression
            - Only call SetExpression when the expression has been tested and works correctly
            - Use TestExpression tool to verify your expression works with the variables
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
}
