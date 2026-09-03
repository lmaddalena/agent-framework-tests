using System;
using System.ComponentModel;
using System.Net.WebSockets;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;


namespace agent_framework_tests;

public static class Session
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Session:");
        Console.WriteLine("(keep context across multiple calls)");
        Console.WriteLine("------------------------------------");

        string modelId = "llama3.2:latest";
        string endpoint = "http://localhost:11434";
       
        // Initialize the chat client using Microsoft.Extensions.AI abstraction
        IChatClient ollamaClient = new OllamaChatClient(new Uri(endpoint), modelId);

        // Create an AI Agent
        AIAgent agent = ollamaClient.AsAIAgent(
            instructions: "You are good at telling jokes.",
            name: "joker"
        );

        // Invoke the agent with a multi-turn conversation, where the context is preserved in the session object.
        AgentSession session = await agent.CreateSessionAsync();
        
        string prompt = "Tell me a joke about a pirate.";        
        Console.WriteLine($"User > {prompt}");
        Console.WriteLine(await agent.RunAsync(prompt, session));

        prompt = "Now add some emojis to the joke and tell it in the voice of a pirate's parrot.";        
        Console.WriteLine($"User > {prompt}");
        Console.WriteLine(await agent.RunAsync(prompt, session));

    }    
}

