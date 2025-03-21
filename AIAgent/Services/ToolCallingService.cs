using System.ComponentModel;
using AIAgent.SKPlugins;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace AIAgent.Services;

public class ToolCallingService
{
    public static async Task CallToolWithKernel()
    {
        var kernel = AiClientFactory.CreateKernel();
        kernel.Plugins.AddFromType<WeatherPlugin>();
        
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory("Do I need an umbrella");
        
        OpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        
        await foreach (var message in chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings: openAiPromptExecutionSettings,
            kernel: kernel))
        {
            Console.Write(message);
        }
    }
    
    public static async Task CallToolWithExtensions()
    {
        var client = AiClientFactory.CreateChatClient();
        
        [Description("Gets the weather")]
        string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
        
        [Description("Gets the wind speed")]
        string GetWindSpeed() => $"{Random.Shared.Next(0,50)} m/s";

        var chatOptions = new ChatOptions
        {
            Tools = 
            [
                AIFunctionFactory.Create(GetWeather),
                AIFunctionFactory.Create(GetWindSpeed)
            ]
        };

        await foreach (var message in client.GetStreamingResponseAsync("Do I need an umbrella?", chatOptions))
        {
            Console.Write(message);
        }
    }
    // 1. Understands that it has access to GetWeather.
    // 2. Decides if it needs to call the function (based on its training and the question).
    // 3. Calls GetWeather and receives a response .
    // 4. Uses the function’s output to generate an answer.
}