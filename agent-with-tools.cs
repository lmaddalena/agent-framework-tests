using System;
using System.ComponentModel;
using System.Net.WebSockets;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;


namespace agent_framework_tests;

public static class AgentWithTools
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Agent with tools:");
        Console.WriteLine("-------------------------");

        string modelId = "llama3.2:latest";
        string endpoint = "http://localhost:11434";
       
        // Initialize the chat client using Microsoft.Extensions.AI abstraction
        IChatClient ollamaClient = new OllamaChatClient(new Uri(endpoint), modelId);

        // Create an AI Agent
        AIAgent agent = ollamaClient.AsAIAgent(
            instructions: "You are a helpful assistant.",
            tools: [AIFunctionFactory.Create(WeatherTool.GetWeather)]
        );

        string prompt = "What is the weather like in Florence?";
        
        Console.WriteLine($"User > {prompt}");

        await foreach (var update in agent.RunStreamingAsync(prompt))
        {
            Console.Write(update);
        }

    }    
}


public static class WeatherTool
{
    [Description("Get the weather for a given location.")]
    public static string GetWeather([Description("The location to get the weather for.")] string location)
        => $"The weather in {location} is cloudy with a high of 20°C.";
}