using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace agent_framework_tests;

public static class Memory
{

    public static async Task RunAsync()
    {
        Console.WriteLine("Memory:");
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
        var logger = loggerFactory.CreateLogger("Memory");

        //logger.LogInformation("test logger");
        
        // Initialize the chat client using Microsoft.Extensions.AI abstraction
        IChatClient ollamaClient = new OllamaChatClient(new Uri(endpoint), modelId)
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        // Creiamo un client dedicato per l'estrazione strutturata delle informazioni di memoria
        IChatClient extractionClient = new OllamaChatClient(new Uri(endpoint), modelId);

        // Creiamo l'agente con le istruzioni e il provider di contesto della memoria personalizzata
        AIAgent agent = ollamaClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                ModelId = modelId,
                Instructions = "You are a friendly assistant. Always address the user by their name.",
            },
            AIContextProviders = [new UserInfoMemory(extractionClient)]
        });

    
        // Creiamo una nuova sessione per la conversazione
        AgentSession session = await agent.CreateSessionAsync();

        Console.WriteLine(">> Use session with blank memory\n");

        // Invochiamo l'agente e stampiamo il risultato
        Console.WriteLine(await agent.RunAsync("Hello, what is the square root of 9?", session));
        Console.WriteLine(await agent.RunAsync("My name is Luca", session));
        Console.WriteLine(await agent.RunAsync("I am 50 years old", session));

        // Serializziamo la sessione (lo stato include anche la memoria)
        JsonElement sessionElement = await agent.SerializeSessionAsync(session);

        Console.WriteLine("\n>> Use deserialized session with previously created memories\n");

        // Deserializziamo la sessione per continuare la conversazione
        var deserializedSession = await agent.DeserializeSessionAsync(sessionElement);
        Console.WriteLine(await agent.RunAsync("What is my name and age?", deserializedSession));

        Console.WriteLine("\n>> Read memories using memory component\n");

        // Accediamo al componente di memoria tramite GetService
        var userInfo = agent.GetService<UserInfoMemory>()?.GetUserInfo(deserializedSession);

        Console.WriteLine($"MEMORY - User Name: {userInfo?.UserName}");
        Console.WriteLine($"MEMORY - User Age: {userInfo?.UserAge}");

        Console.WriteLine("\n>> Use new session with previously created memories\n");

        var newSession = await agent.CreateSessionAsync();
        if (userInfo is not null && agent.GetService<UserInfoMemory>() is UserInfoMemory newSessionMemory)
        {
            newSessionMemory.SetUserInfo(newSession, userInfo);
        }

        Console.WriteLine(await agent.RunAsync("What is my name and age?", newSession));
    }
}

/// <summary>
/// Sample memory component that can remember a user's name and age.
/// </summary>
internal sealed class UserInfoMemory : AIContextProvider
{
    private readonly ProviderSessionState<UserInfo> _sessionState;
    private IReadOnlyList<string>? _stateKeys;
    private readonly IChatClient _chatClient;

    public UserInfoMemory(IChatClient chatClient, Func<AgentSession?, UserInfo>? stateInitializer = null)
    {
        this._sessionState = new ProviderSessionState<UserInfo>(
            stateInitializer ?? (_ => new UserInfo()),
            this.GetType().Name);
        this._chatClient = chatClient;
    }

    public override IReadOnlyList<string> StateKeys => this._stateKeys ??= [this._sessionState.StateKey];

    public UserInfo GetUserInfo(AgentSession session)
        => this._sessionState.GetOrInitializeState(session);

    public void SetUserInfo(AgentSession session, UserInfo userInfo)
        => this._sessionState.SaveState(session, userInfo);

    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        var userInfo = this._sessionState.GetOrInitializeState(context.Session);

        // Try and extract the user name and age from the message if we don't have it already and it's a user message.
        if ((userInfo.UserName is null || userInfo.UserAge is null) && context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            // The Foundry Responses API requires the model name in the request body.
            // Retrieve it from the client's metadata so callers don't need to pass it separately.
            var modelId = this._chatClient.GetService<ChatClientMetadata>()?.DefaultModelId
                ?? throw new InvalidOperationException(
                    "Could not retrieve DefaultModelId from the extraction IChatClient. " +
                    "Ensure the client was created with a model ID (e.g., via projectClient.AsAIAgent(...)).");
            var result = await this._chatClient.GetResponseAsync<UserInfo>(
                context.RequestMessages,
                new ChatOptions()
                {
                    ModelId = modelId,
                    Instructions = "Extract the user's name and age from the message if present. If not present return nulls."
                },
                cancellationToken: cancellationToken);

            userInfo.UserName ??= result.Result.UserName;
            userInfo.UserAge ??= result.Result.UserAge;
        }

        this._sessionState.SaveState(context.Session, userInfo);
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var userInfo = this._sessionState.GetOrInitializeState(context.Session);

        StringBuilder instructions = new();

        // If we don't already know the user's name and age, add instructions to ask for them, otherwise just provide what we have to the context.
        instructions
            .AppendLine(
                userInfo.UserName is null ?
                    "Ask the user for their name and politely decline to answer any questions until they provide it." :
                    $"The user's name is {userInfo.UserName}.")
            .AppendLine(
                userInfo.UserAge is null ?
                    "Ask the user for their age and politely decline to answer any questions until they provide it." :
                    $"The user's age is {userInfo.UserAge}.");

        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = instructions.ToString()
        });
    }
}

internal sealed class UserInfo
{
    public string? UserName { get; set; }
    public int? UserAge { get; set; }
}