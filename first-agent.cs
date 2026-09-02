using System.Net.WebSockets;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace agent_framework_tests;

public static class FirstAgent
{

    public static async Task RunAsync()
    {
        Console.WriteLine("First Agent:");
        Console.WriteLine("-------------------------");

        string modelId = "llama3.2:latest";
        string endpoint = "http://localhost:11434";

        // Setup logger factory for Microsoft.Extensions.AI
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Error);
        });

        // create the logger
        var logger = loggerFactory.CreateLogger("FirstAgent");

        //logger.LogInformation("test logger");
        
        // Initialize the chat client using Microsoft.Extensions.AI abstraction
        IChatClient ollamaClient = new OllamaChatClient(new Uri(endpoint), modelId)
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        // Create an AI Agent
        AIAgent agent = ollamaClient.AsAIAgent(
            instructions: "You are a friendly assistant. Keep your answers brief.",
            name: "HelloAgent"
        );

        string prompt = "What is the largest city in France?";
        
        Console.WriteLine($"User > {prompt}");

        Console.WriteLine($"agent > {await agent.RunAsync(prompt)}");

        //...or streaming response
        // await foreach (var update in agent.RunStreamingAsync(prompt))
        // {
        //     Console.Write(update);
        // }
    }
}