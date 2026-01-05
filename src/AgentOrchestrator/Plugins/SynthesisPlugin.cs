using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace AgentOrchestrator.Plugins;

public class SynthesisPlugin
{
    private readonly Kernel _kernel;

    public SynthesisPlugin(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Synthesizes multiple agent responses into a coherent unified response")]
    public async Task<string> Synthesize(
        [Description("The original user query")] string originalQuery,
        [Description("JSON array of agent responses to synthesize")] string responses,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            You are a response synthesizer. Your job is to combine multiple agent responses into a single,
            coherent response that addresses the user's original query.

            Original User Query: {originalQuery}

            Agent Responses:
            {responses}

            Instructions:
            1. Analyze all the agent responses
            2. Combine them into a single, well-organized response
            3. Maintain clear structure - if there are multiple topics, organize them with headers or clear transitions
            4. Remove any redundancy between responses
            5. Ensure the response directly addresses the user's original query
            6. Keep the tone helpful and conversational
            7. If one response is about M365 data (emails, calendar, etc.) and another is general knowledge,
               present the M365 data first, then the general information

            Synthesized Response:
            """;

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.GetValue<string>() ?? "I couldn't synthesize a response.";
    }
}
