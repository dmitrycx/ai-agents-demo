using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.AI;

namespace AIAgent.Services;

public class CodeExecutionService
{
    public static async Task ExecuteCodeWithExtensions()
    {
        var client = AiClientFactory.CreateChatClient();
        
        [Description("Executes a C# expression and returns the result.")]
        async Task<string> ExecuteCSharpCode(string code)
        {
            try
            {
                Console.WriteLine("executing ExecuteCSharpCode method");
                var result = await CSharpScript.EvaluateAsync<object>(code, ScriptOptions.Default);
                //var result = await ExecuteSandboxedCodeAsync(code);
                return result?.ToString() ?? "null";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        
        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(ExecuteCSharpCode)
            ]
        };
        
        var chatMessage = "Evaluate: 2 + 2 * 3";
        await foreach (var message in client.GetStreamingResponseAsync(chatMessage, chatOptions))
        {
            Console.Write(message);
        }
    }
    
    private static async Task<object> ExecuteSandboxedCodeAsync(string code)
    {
        Console.WriteLine("executing ExecuteCSharpCode method safely");
        // Define sandboxed script options
        var scriptOptions = ScriptOptions.Default
            .WithImports("System") // Restrict imports to only safe namespaces
            .WithReferences(AppDomain.CurrentDomain.GetAssemblies());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Limit execution time

        try
        {
            return await CSharpScript.EvaluateAsync<object>(code, scriptOptions, cancellationToken: cts.Token);
        }
        catch (CompilationErrorException ex)
        {
            return $"Compilation Error: {string.Join("\n", ex.Diagnostics)}";
        }
        catch (Exception ex)
        {
            return $"Execution Error: {ex.Message}";
        }
    }
}