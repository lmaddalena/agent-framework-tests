using System.Net.WebSockets;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using CommunityToolkit.VectorData.InMemory;
namespace agent_framework_tests;

public static class Rag
{

    public static async Task RunAsync()
    {
        Console.WriteLine("RAG:");
        Console.WriteLine("-------------------------");

        string modelId = "llama3.2:latest";
        string embeddingModelName = "nomic-embed-text";
        string endpoint = "http://localhost:11434";

        // 1. Inizializzazione dei client IChatClient e IEmbeddingGenerator usando Microsoft.Extensions.AI con Ollama
        IChatClient chatClient = new OllamaChatClient(endpoint, modelId);
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaEmbeddingGenerator(endpoint, embeddingModelName);
        
        // Nota: La dimensione del vettore dipende dal modello di embedding scelto (es. nomic-embed-text usa 768 dimensioni)
        int embeddingDimensions = 768;

        //var embeddings = await embeddingGenerator.GenerateAsync(["test"]);
        //int dimensions = embeddings.First().Vector.Length;
        //Console.WriteLine($"Dimensione vettori: {dimensions}");

        // 2. Creazione di un Vector Store in-memory che usa il generatore di embedding di Ollama
        VectorStore vectorStore = new InMemoryVectorStore(new()
        {
            EmbeddingGenerator = embeddingGenerator
        });

        // Creazione dello store per i documenti di ricerca
        TextSearchStore textSearchStore = new(vectorStore, "product-and-policy-info", embeddingDimensions);

        // Caricamento dei documenti di esempio nello store
        await textSearchStore.UpsertDocumentsAsync(GetSampleDocuments());

        // Funzione di adattamento per la ricerca
        async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapterAsync(string text, CancellationToken ct)
        {
            var searchResults = await textSearchStore.SearchAsync(text, 1, ct);
            return searchResults.Select(r => new TextSearchProvider.TextSearchResult
            {
                SourceName = r.SourceName,
                SourceLink = r.SourceLink,
                Text = r.Text ?? string.Empty,
                RawRepresentation = r
            });
        }

        // Opzioni per il provider di ricerca testuale
        TextSearchProviderOptions textSearchOptions = new()
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        };        
        
        // 3. Creazione dell'agente AI
        AIAgent agent = chatClient
            .AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new() 
                { 
                    Instructions = "You are a helpful support specialist for Contoso Outdoors. Answer questions using the provided context and cite the source document when available." 
                },
                AIContextProviders = [new TextSearchProvider(SearchAdapterAsync, textSearchOptions)],
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
                {
                    StorageInputRequestMessageFilter = messages => messages.Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.AIContextProvider && m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory)
                }),
            });        
        
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine(">> Asking about returns\n");
            Console.WriteLine(await agent.RunAsync("Hi! I need help understanding the return policy.", session));

            Console.WriteLine("\n>> Asking about shipping\n");
            Console.WriteLine(await agent.RunAsync("How long does standard shipping usually take?", session));

            Console.WriteLine("\n>> Asking about product care\n");
            Console.WriteLine(await agent.RunAsync("What is the best way to maintain the TrailRunner tent fabric?", session));        
    }

    // Documenti di esempio
    static IEnumerable<TextSearchDocument> GetSampleDocuments()
    {
        yield return new TextSearchDocument
        {
            SourceId = "return-policy-001",
            SourceName = "Contoso Outdoors Return Policy",
            SourceLink = "https://contoso.com/policies/returns",
            Text = "Customers may return any item within 30 days of delivery. Items should be unused and include original packaging. Refunds are issued to the original payment method within 5 business days of inspection."
        };
        yield return new TextSearchDocument
        {
            SourceId = "shipping-guide-001",
            SourceName = "Contoso Outdoors Shipping Guide",
            SourceLink = "https://contoso.com/help/shipping",
            Text = "Standard shipping is free on orders over $50 and typically arrives in 3-5 business days within the continental United States. Expedited options are available at checkout."
        };
        yield return new TextSearchDocument
        {
            SourceId = "tent-care-001",
            SourceName = "TrailRunner Tent Care Instructions",
            SourceLink = "https://contoso.com/manuals/trailrunner-tent",
            Text = "Clean the tent fabric with lukewarm water and a non-detergent soap. Allow it to air dry completely before storage and avoid prolonged UV exposure to extend the lifespan of the waterproof coating."
        };
    }    
}