using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIAgent.Services;

public class MultiAgentService
{

    [Experimental("SKEXP0110")]
    public static async Task CallMultiAgentChat()
    {
        var kernel = AiClientFactory.CreateKernel();
        
        string ProgamManager = """
                                   You are a program manager which will take the requirement and create a plan for creating app. 
                                   Program Manager understands the user requirements and form the detail documents with requirements and costing. 
                               """;

        string SoftwareEngineer = """
                                     You are Software Engineer, and your goal is create web app using .NET by taking into consideration all
                                     the requirements given by Program Manager. 
                                  """;

        string ProjectManager = """
                                    You are manager which will review software engineer code, and make sure all client requirements are completed. 
                                    You are the guardian of quality, ensuring the final product meets all specifications and receives the green light for release.
                                     Once all client requirements are completed, you can approve the request by just responding "approve"
                                """;
        
        ChatCompletionAgent ProgramManagerAgent =
            new()
            {
                Instructions = ProgamManager,
                Name = "ProgramManagerAgent",
                Kernel = kernel
            };

        ChatCompletionAgent SoftwareEngineerAgent =
            new()
            {
                Instructions = SoftwareEngineer,
                Name = "SoftwareEngineerAgent",
                Kernel = kernel
            };

        ChatCompletionAgent ProjectManagerAgent =
            new()
            {
                Instructions = ProjectManager,
                Name = "ProjectManagerAgent",
                Kernel = kernel
            };
        
        
        AgentGroupChat chat =
            new(ProgramManagerAgent, SoftwareEngineerAgent, ProjectManagerAgent)
            {
                ExecutionSettings =
                    new()
                    {
                        TerminationStrategy =
                            new ApprovalTerminationStrategy()
                            {
                                Agents = [ProjectManagerAgent],
                                MaximumIterations = 6,
                            }
                    }
            };
        
        // Invoke chat and display messages.
        var input = """
                    I want to develop app which will provide me calculator. Keep it very simple. And get final approval from manager.
                    """;

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
        Console.WriteLine($"# {AuthorRole.User}: '{input}'");

        await foreach (var content in chat.InvokeAsync())
        {
            Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
        }
    }
}

[Experimental("SKEXP0110")]
sealed class ApprovalTerminationStrategy : TerminationStrategy
{
    // Terminate when the final message contains the term "approve"
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
}
