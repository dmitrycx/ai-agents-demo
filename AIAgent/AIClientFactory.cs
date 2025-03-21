using System.ClientModel;
using AIAgent.SKPlugins;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace AIAgent;

public static class AiClientFactory
{
    private static readonly string Endpoint;
    private static readonly string Key;
    private static readonly string ChatModelId;
    private static readonly string ChatMiniModelId;
    private static readonly string EmbeddingModelId;

    static AiClientFactory()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();
        Endpoint = config["AZURE_OPENAI_ENDPOINT"]!;
        Key = config["AZURE_OPENAI_API_KEY"]!;
        ChatModelId = "gpt-4o";
        ChatMiniModelId = "gpt-4o-mini";
        EmbeddingModelId = "text-embedding-3-small";
    }

    public static Kernel CreateKernel()
    {
        return Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(ChatModelId, Endpoint, Key)
            .Build();
    }

    public static IChatClient CreateChatClient()
    {
        return new AzureOpenAIClient(new Uri(Endpoint), new ApiKeyCredential(Key))
            .AsChatClient(ChatMiniModelId)
            .AsBuilder()
            .UseFunctionInvocation() // allows to call registered functions
            .Build();
    }
    
    public static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
    {
        return new AzureOpenAIClient(
                new Uri(Endpoint),
                new ApiKeyCredential(Key))
            .AsEmbeddingGenerator(EmbeddingModelId);
    }
}