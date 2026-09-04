using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace agent_framework_tests;

public static class Plugin
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Plugin with DI:");
        Console.WriteLine("------------------------------------");

        string modelId = "llama3.2:latest";
        string endpoint = "http://localhost:11434";
       

        // Create a service collection to hold the agent plugin and its dependencies.
        ServiceCollection services = new();
        services.AddSingleton<WeatherProvider>();
        services.AddSingleton<CurrentTimeProvider>();
        services.AddSingleton<AgentPlugin>(); // The plugin depends on WeatherProvider and CurrentTimeProvider registered above.       

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // Initialize the chat client using Microsoft.Extensions.AI abstraction
        IChatClient ollamaClient = new OllamaChatClient(new Uri(endpoint), modelId);

        // Create an AI Agent
        AIAgent agent = ollamaClient.AsAIAgent(
            instructions: "You are good at telling jokes.",
            name: "joker",
            tools: [.. serviceProvider.GetRequiredService<AgentPlugin>().AsAITools()],
            services: serviceProvider // Pass the service provider to the agent so it will be available to plugin functions to resolve dependencies.
        );

        string prompt = "Tell me current time and weather in Seattle.";        
        Console.WriteLine($"User > {prompt}");
        Console.WriteLine(await agent.RunAsync("Tell me current time and weather in Seattle."));

    }    
}

/// <summary>
/// The agent plugin that provides weather and current time information.
/// </summary>
/// <param name="weatherProvider">The weather provider to get weather information.</param>
internal sealed class AgentPlugin(WeatherProvider weatherProvider)
{
    /// <summary>
    /// Gets the weather information for the specified location.
    /// </summary>
    /// <remarks>
    /// This method demonstrates how to use the dependency that was injected into the plugin class.
    /// </remarks>
    /// <param name="location">The location to get the weather for.</param>
    /// <returns>The weather information for the specified location.</returns>
    public string GetWeather(string location)
    {
        return weatherProvider.GetWeather(location);
    }

    /// <summary>
    /// Gets the current date and time for the specified location.
    /// </summary>
    /// <remarks>
    /// This method demonstrates how to resolve a dependency using the service provider passed to the method.
    /// </remarks>
    /// <param name="sp">The service provider to resolve the <see cref="CurrentTimeProvider"/>.</param>
    /// <param name="location">The location to get the current time for.</param>
    /// <returns>The current date and time as a <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset GetCurrentTime(IServiceProvider sp, string location)
    {
        // Resolve the CurrentTimeProvider from the service provider
        var currentTimeProvider = sp.GetRequiredService<CurrentTimeProvider>();

        return currentTimeProvider.GetCurrentTime(location);
    }

    /// <summary>
    /// Returns the functions provided by this plugin.
    /// </summary>
    /// <remarks>
    /// In real world scenarios, a class may have many methods and only a subset of them may be intended to be exposed as AI functions.
    /// This method demonstrates how to explicitly specify which methods should be exposed to the AI agent.
    /// </remarks>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(this.GetWeather);
        yield return AIFunctionFactory.Create(this.GetCurrentTime);
    }
}

/// <summary>
/// The weather provider that returns weather information.
/// </summary>
internal sealed class WeatherProvider
{
    /// <summary>
    /// Gets the weather information for the specified location.
    /// </summary>
    /// <remarks>
    /// The weather information is hardcoded for demonstration purposes.
    /// In a real application, this could call a weather API to get actual weather data.
    /// </remarks>
    /// <param name="location">The location to get the weather for.</param>
    /// <returns>The weather information for the specified location.</returns>
    public string GetWeather(string location)
    {
        return $"The weather in {location} is cloudy with a high of 15°C.";
    }
}

/// <summary>
/// Provides the current date and time.
/// </summary>
/// <remarks>
/// This class returns the current date and time using the system's clock.
/// </remarks>
internal sealed class CurrentTimeProvider
{
    /// <summary>
    /// Gets the current date and time.
    /// </summary>
    /// <param name="location">The location to get the current time for (not used in this implementation).</param>
    /// <returns>The current date and time as a <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset GetCurrentTime(string location)
    {
        return DateTimeOffset.Now;
    }
}