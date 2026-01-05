using System.Text.Json;
using AgentOrchestrator.Auth;
using AgentOrchestrator.Models;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.SemanticKernel;

namespace AgentOrchestrator.Agent;

public class OrchestratorAgent : AgentApplication
{
    private readonly Kernel _kernel;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        AgentApplicationOptions options,
        Kernel kernel,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrchestratorAgent> logger) : base(options)
    {
        _kernel = kernel;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        // Register activity handlers
        OnActivity(ActivityTypes.Message, OnMessageActivityAsync);
        OnActivity(ActivityTypes.ConversationUpdate, OnConversationUpdateAsync);
    }

    private async Task OnMessageActivityAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        CancellationToken cancellationToken)
    {
        var userMessage = turnContext.Activity.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(userMessage))
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Please enter a message."),
                cancellationToken);
            return;
        }

        _logger.LogInformation("Processing message: {Message}", userMessage);

        try
        {
            // Get user's access token for M365 Copilot calls
            var accessToken = await GetUserAccessTokenAsync(turnContext, cancellationToken);

            // Step 1: Analyze intent
            _logger.LogInformation("Step 1: Analyzing intent...");
            var intents = await AnalyzeIntentAsync(userMessage, cancellationToken);
            _logger.LogInformation("Detected {Count} intent(s): {Intents}",
                intents.Count,
                string.Join(", ", intents.Select(i => i.Type)));

            // Step 2: Execute agents in parallel based on intents
            _logger.LogInformation("Step 2: Executing agents...");
            var responses = await ExecuteAgentsAsync(intents, accessToken, cancellationToken);

            // Step 3: Synthesize response
            _logger.LogInformation("Step 3: Synthesizing response...");
            var finalResponse = await SynthesizeResponseAsync(userMessage, responses, cancellationToken);

            // Step 4: Send response
            await turnContext.SendActivityAsync(
                MessageFactory.Text(finalResponse),
                cancellationToken);

            _logger.LogInformation("Response sent successfully");
        }
        catch (UnauthorizedAccessException)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Please log in to access M365 features. Visit the web interface to authenticate."),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await turnContext.SendActivityAsync(
                MessageFactory.Text($"Sorry, an error occurred: {ex.Message}"),
                cancellationToken);
        }
    }

    private async Task<List<Intent>> AnalyzeIntentAsync(string query, CancellationToken cancellationToken)
    {
        var result = await _kernel.InvokeAsync(
            "IntentPlugin",
            "AnalyzeIntent",
            new() { ["query"] = query },
            cancellationToken);

        var json = result.GetValue<string>() ?? "[]";

        try
        {
            var intents = JsonSerializer.Deserialize<List<Intent>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return intents ?? [new Intent { Type = IntentType.GeneralKnowledge, Query = query }];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse intents, defaulting to general knowledge");
            return [new Intent { Type = IntentType.GeneralKnowledge, Query = query }];
        }
    }

    private async Task<List<AgentResponse>> ExecuteAgentsAsync(
        List<Intent> intents,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var tasks = intents.Select(intent => ExecuteAgentForIntentAsync(intent, accessToken, cancellationToken));
        var responses = await Task.WhenAll(tasks);
        return responses.ToList();
    }

    private async Task<AgentResponse> ExecuteAgentForIntentAsync(
        Intent intent,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            return intent.Type switch
            {
                IntentType.M365Email => await ExecuteM365PluginAsync("QueryEmails", intent.Query, accessToken, cancellationToken),
                IntentType.M365Calendar => await ExecuteM365PluginAsync("QueryCalendar", intent.Query, accessToken, cancellationToken),
                IntentType.M365Files => await ExecuteM365PluginAsync("QueryFiles", intent.Query, accessToken, cancellationToken),
                IntentType.M365People => await ExecuteM365PluginAsync("QueryPeople", intent.Query, accessToken, cancellationToken),
                IntentType.GeneralKnowledge => await ExecuteGeneralKnowledgeAsync(intent.Query, cancellationToken),
                _ => new AgentResponse
                {
                    Agent = "unknown",
                    IntentType = intent.Type,
                    Content = "I'm not sure how to handle that request.",
                    Success = false
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent for intent {IntentType}", intent.Type);
            return new AgentResponse
            {
                Agent = intent.Type.ToString(),
                IntentType = intent.Type,
                Content = $"Error: {ex.Message}",
                Success = false
            };
        }
    }

    private async Task<AgentResponse> ExecuteM365PluginAsync(
        string functionName,
        string query,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var result = await _kernel.InvokeAsync(
            "M365CopilotPlugin",
            functionName,
            new()
            {
                ["query"] = query,
                ["accessToken"] = accessToken
            },
            cancellationToken);

        return new AgentResponse
        {
            Agent = "m365_copilot",
            IntentType = functionName switch
            {
                "QueryEmails" => IntentType.M365Email,
                "QueryCalendar" => IntentType.M365Calendar,
                "QueryFiles" => IntentType.M365Files,
                "QueryPeople" => IntentType.M365People,
                _ => IntentType.GeneralKnowledge
            },
            Content = result.GetValue<string>() ?? string.Empty,
            Success = true
        };
    }

    private async Task<AgentResponse> ExecuteGeneralKnowledgeAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var result = await _kernel.InvokeAsync(
            "AzureOpenAIPlugin",
            "GeneralKnowledge",
            new() { ["query"] = query },
            cancellationToken);

        return new AgentResponse
        {
            Agent = "azure_openai",
            IntentType = IntentType.GeneralKnowledge,
            Content = result.GetValue<string>() ?? string.Empty,
            Success = true
        };
    }

    private async Task<string> SynthesizeResponseAsync(
        string originalQuery,
        List<AgentResponse> responses,
        CancellationToken cancellationToken)
    {
        // If only one successful response, return it directly
        var successfulResponses = responses.Where(r => r.Success).ToList();
        if (successfulResponses.Count == 1)
        {
            return successfulResponses[0].Content;
        }

        if (successfulResponses.Count == 0)
        {
            return "I wasn't able to find an answer to your question.";
        }

        // Multiple responses - synthesize them
        var responsesJson = JsonSerializer.Serialize(successfulResponses.Select(r => new
        {
            agent = r.Agent,
            content = r.Content
        }));

        var result = await _kernel.InvokeAsync(
            "SynthesisPlugin",
            "Synthesize",
            new()
            {
                ["originalQuery"] = originalQuery,
                ["responses"] = responsesJson
            },
            cancellationToken);

        return result.GetValue<string>() ?? "I couldn't synthesize a response.";
    }

    private async Task<string> GetUserAccessTokenAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        // For web channel, get token from HTTP context session
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var sessionId = httpContext.Session.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                try
                {
                    var token = await _tokenService.GetAccessTokenAsync(sessionId);
                    if (!string.IsNullOrEmpty(token))
                    {
                        return token;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Token not found, fall through to throw unauthorized
                }
            }
        }

        // For Teams/other channels, token would be passed via activity or SSO
        // This would be implemented when adding Teams channel support
        var tokenFromActivity = turnContext.Activity.Value?.ToString();
        if (!string.IsNullOrEmpty(tokenFromActivity))
        {
            return tokenFromActivity;
        }

        throw new UnauthorizedAccessException("No access token available. Please log in.");
    }

    private async Task OnConversationUpdateAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        CancellationToken cancellationToken)
    {
        var membersAdded = turnContext.Activity.MembersAdded;
        if (membersAdded != null)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text(
                            "Welcome to the .NET 10 Agent! I can help you with M365 data (emails, calendar, files, people) " +
                            "and general knowledge questions. Try asking something like:\n\n" +
                            "- \"Summarize my emails from today\"\n" +
                            "- \"What meetings do I have tomorrow?\"\n" +
                            "- \"Explain what microservices are\""),
                        cancellationToken);
                }
            }
        }
    }
}
