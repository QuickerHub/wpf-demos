using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Realtime;
using OpenAI.Responses;
using QuickerExpressionAgent.Demo.Plugins;

// Load configuration
// Try multiple paths for appsettings.json
var configPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
    Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")
};

var configBuilder = new ConfigurationBuilder();
foreach (var path in configPaths)
{
    if (File.Exists(path))
    {
        configBuilder.AddJsonFile(path, optional: true);
        break;
    }
}

var configuration = configBuilder
    .AddEnvironmentVariables()
    .Build();

// Get settings from config
var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
var baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
var modelId = configuration["OpenAI:ModelId"] ?? "gpt-4";

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Error: API key not found. Please set it in appsettings.json or OPENAI_API_KEY environment variable.");
    return;
}

// Create kernel with custom base URL
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOpenAIChatCompletion(
    modelId: modelId,
    apiKey: apiKey,
    endpoint: new Uri(baseUrl));

// Add plugins
var calculatorPlugin = new CalculatorPlugin();
kernelBuilder.Plugins.AddFromObject(calculatorPlugin, "Calculator");

var timePlugin = new TimePlugin();
kernelBuilder.Plugins.AddFromObject(timePlugin, "Time");

var kernel = kernelBuilder.Build();

Console.WriteLine("=== Semantic Kernel Agent Demo ===");
Console.WriteLine("This demo shows how to use Semantic Kernel with plugins.");
Console.WriteLine("The AI can use Calculator and Time tools to help you.");
Console.WriteLine("Type 'exit' to quit.\n");

// Create chat history for conversation
var chatHistory = new ChatHistory();

// Get chat completion service
var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

// Simple chat loop
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
        // Add user message to chat history
        chatHistory.AddUserMessage(userInput);

        // Store initial history count to detect new messages
        int initialHistoryCount = chatHistory.Count;

        // Track the last processed message index to detect new tool call results in real-time
        int lastProcessedMessageIndex = initialHistoryCount;

        // Configure execution settings to auto-invoke kernel functions
        // This will automatically execute tool calls and add results to chat history
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var lastMessageCount = chatHistory.Count;

        await foreach (var chatUpdate in chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel,
            CancellationToken.None))
        {
            if (chatHistory.Count > lastMessageCount)
            {
                // New message(s) added to chat history (likely tool call results)
                Console.WriteLine();
                for (int i = lastMessageCount; i < chatHistory.Count; i++)
                {
                    var newMessage = chatHistory[i];
                    if (newMessage.Role == AuthorRole.Tool)
                    {
                        Console.Write($"[Tool Call Result] {newMessage.Content}");
                    }
                }
                lastMessageCount = chatHistory.Count;
            }

            // Real-time streaming output - print incremental content
            if (!string.IsNullOrEmpty(chatUpdate.Content))
            {
                Console.Write(chatUpdate.Content);
            }
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
                    Console.Write(update.Arguments); // 增量打印参数  
                }
            }
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}\n");
    }
}

Console.WriteLine("Goodbye!");
