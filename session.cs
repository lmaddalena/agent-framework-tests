using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text.Json;
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

        // Serialize the session state to a JsonElement, so it can be stored for later use.
        JsonElement serializedSession = await agent.SerializeSessionAsync(session);
        
        // In a real application, you would typically write the serialized session to a file or
        // database for persistence, and read it back when resuming the conversation.
        // Here we'll just write the serialized session to console (for demonstration purposes).
        Console.WriteLine("\n--- Serialized session ---\n");
        Console.WriteLine(JsonSerializer.Serialize(serializedSession, new JsonSerializerOptions { WriteIndented = true }) + "\n");

        // Deserialize the session state after loading from storage.
        AgentSession resumedSession = await agent.DeserializeSessionAsync(serializedSession);

        // Run the agent again with the resumed session.
        prompt = "Now add some emojis to the joke and tell it in the voice of a pirate's parrot.";        
        Console.WriteLine($"User > {prompt}");
        Console.WriteLine(await agent.RunAsync(prompt, resumedSession));

        Console.WriteLine("\n--- Serialized session ---\n");
        serializedSession = await agent.SerializeSessionAsync(resumedSession);
        Console.WriteLine(JsonSerializer.Serialize(serializedSession, new JsonSerializerOptions { WriteIndented = true }) + "\n");

    }    
}

