using System.ComponentModel;
using ChromaDB.Client;
using ChromaDB.Client.Models;
using Microsoft.Extensions.AI;

namespace AIAgent.Services;

public class ContextAiService
{
    public static async Task CallToolWithoutContext()
    {
        var client = AiClientFactory.CreateChatClient();
        
        [Description("Gets the weather")]
        async Task<string> GetWeather()
        {
            return $"It's {(Random.Shared.NextDouble() > 0.5 ? "sunny" : "raining")}.";
        }

        var chatOptions = new ChatOptions
        {
            Tools = 
            [
                AIFunctionFactory.Create(GetWeather)
            ]
        };

        await foreach (var message in client.GetStreamingResponseAsync("Book a flight to NY.", chatOptions))
        {
            Console.Write(message);
        }
        Console.WriteLine();
        
        await foreach (var message in client.GetStreamingResponseAsync("What should I wear on my vacation?", chatOptions))
        {
            Console.Write(message);
        }
    }
    
    public static async Task CallToolWithContext()
    {
        var client = AiClientFactory.CreateChatClient();

        [Description("Gets the weather")]
        string GetWeather(string destination)
        {
            return $"It's {(Random.Shared.NextDouble() > 0.5 ? "sunny" : "raining")} in {destination}";
        }

        [Description("Books a flight to a given destination")]
        async Task<string> BookFlight(string userRequest, string destination)
        {
            await StoreFlightBooking(userRequest, destination);
            return $"Your flight to {destination} is booked.";
        }
        
        [Description("Gets travel-related advice")]
        async Task<string> GetTravelAdvice(string userRequest)
        {
            var response = await FetchRelevantAdvice(userRequest);
            return response;
        }
        
        var chatOptions = new ChatOptions
        {
            Tools = 
            [
                AIFunctionFactory.Create(GetWeather),
                AIFunctionFactory.Create(BookFlight),
                AIFunctionFactory.Create(GetTravelAdvice)
            ],
            ToolMode = ChatToolMode.RequireAny
        };
        
        await foreach (var message in client.GetStreamingResponseAsync("Book a flight to NY.", chatOptions))
        {
            Console.Write(message);
        }
        Console.WriteLine();
        
        await foreach (var message in client.GetStreamingResponseAsync("What should I wear on my vacation?", chatOptions))
        {
            Console.Write(message);
        }
    }

    
    private static async Task StoreFlightBooking(string userRequest, string destination)
    {
        var response = $"Your flight to {destination} is booked.";

        var collectionClient = await GetChromaCollectionClient();
        var generator = AiClientFactory.CreateEmbeddingGenerator();
        var embedding = await generator.GenerateEmbeddingVectorAsync(userRequest);

        await collectionClient.Add(
            [Guid.NewGuid().ToString()],
            [embedding],
            [
                new Dictionary<string, object>
                {
                    ["Type"] = "FlightBooking",
                    ["Destination"] = destination,
                    ["UserRequest"] = userRequest // Store the original request as metadata
                }
            ]
        );
    }
    
    private static async Task<string> FetchRelevantAdvice(string userRequest)
    {
        var collectionClient = await GetChromaCollectionClient();
        var generator = AiClientFactory.CreateEmbeddingGenerator();
        var embedding = await generator.GenerateEmbeddingVectorAsync(userRequest);

        // Query ChromaDB using embedding, requesting the top 1 result based on similarity
        var searchResults = await collectionClient.Query(embedding , 1, include: ChromaQueryInclude.Distances | ChromaQueryInclude.Metadatas);

        if (searchResults.Any())
        {
            var metadata = searchResults.First().Metadata;
            var destination = metadata!["Destination"].ToString();
            var userRequestContext = metadata["UserRequest"].ToString();

            // Leaving it up to the AI to call the `GetWeather` function using description
            return $"Based on your trip to {destination} and your previous request '{userRequestContext}', let me get the current weather for you.";
        }
        else
        {
            return "I'm sorry, I couldn't find any related travel advice for your request.";
        }
    }
    
    private static async Task<ChromaCollectionClient> GetChromaCollectionClient()
    {
        var configOptions = new ChromaConfigurationOptions(uri: "http://localhost:8000/api/v1/");
        var httpClient = new HttpClient();
        var chromaClient = new ChromaClient(configOptions, httpClient);
        var collection = await chromaClient.GetOrCreateCollection("chat_memory");
        return new ChromaCollectionClient(collection, configOptions, httpClient);
    }
}