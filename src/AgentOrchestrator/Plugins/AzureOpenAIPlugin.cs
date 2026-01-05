using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace AgentOrchestrator.Plugins;

public class AzureOpenAIPlugin
{
    private readonly Kernel _kernel;

    public AzureOpenAIPlugin(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Answers general knowledge questions that are not related to Microsoft 365 data")]
    public async Task<string> GeneralKnowledge(
        [Description("The general knowledge question to answer")] string query,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            You are a helpful AI assistant. Answer the following question clearly and concisely.

            Question: {query}

            Provide a helpful, accurate, and informative response. If the question is about a technical topic,
            include relevant details and examples where appropriate.
            """;

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.GetValue<string>() ?? "I couldn't generate a response.";
    }
}
