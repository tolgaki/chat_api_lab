using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace AgentOrchestrator.Plugins;

public class AzureOpenAIPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<AzureOpenAIPlugin>? _logger;

    public AzureOpenAIPlugin(Kernel kernel, ILogger<AzureOpenAIPlugin>? logger = null)
    {
        _kernel = kernel;
        _logger = logger;
    }

    [KernelFunction]
    [Description("Answers general knowledge questions that are not related to Microsoft 365 data")]
    public async Task<string> GeneralKnowledge(
        [Description("The general knowledge question to answer")] string query,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Processing general knowledge query: {Query}", query);
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
