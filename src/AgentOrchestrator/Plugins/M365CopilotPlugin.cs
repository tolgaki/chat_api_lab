using System.ComponentModel;
using AgentOrchestrator.CopilotSdk;
using AgentOrchestrator.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.SemanticKernel;
using CopilotChatRequest = AgentOrchestrator.CopilotSdk.Models.ChatRequest;
using CopilotMessageParameter = AgentOrchestrator.CopilotSdk.Models.MessageParameter;
using CopilotLocationHint = AgentOrchestrator.CopilotSdk.Models.LocationHint;

namespace AgentOrchestrator.Plugins;

public class M365CopilotPlugin
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MicrosoftGraphSettings _graphSettings;
    private readonly ILogger<M365CopilotPlugin> _logger;

    public M365CopilotPlugin(
        IHttpClientFactory httpClientFactory,
        MicrosoftGraphSettings graphSettings,
        ILogger<M365CopilotPlugin> logger)
    {
        _httpClientFactory = httpClientFactory;
        _graphSettings = graphSettings;
        _logger = logger;
    }

    [KernelFunction]
    [Description("Query Microsoft 365 Copilot for email-related questions using the Chat API")]
    public async Task<string> QueryEmails(
        [Description("The email-related question")] string query,
        [Description("User's access token for Graph API")] string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await CallCopilotChatApiAsync(query, accessToken, cancellationToken);
    }

    [KernelFunction]
    [Description("Query Microsoft 365 Copilot for calendar-related questions using the Chat API")]
    public async Task<string> QueryCalendar(
        [Description("The calendar-related question")] string query,
        [Description("User's access token for Graph API")] string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await CallCopilotChatApiAsync(query, accessToken, cancellationToken);
    }

    [KernelFunction]
    [Description("Query Microsoft 365 Copilot for file and document questions using the Chat API")]
    public async Task<string> QueryFiles(
        [Description("The files-related question")] string query,
        [Description("User's access token for Graph API")] string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await CallCopilotChatApiAsync(query, accessToken, cancellationToken);
    }

    [KernelFunction]
    [Description("Query Microsoft 365 Copilot for people and organization questions using the Chat API")]
    public async Task<string> QueryPeople(
        [Description("The people-related question")] string query,
        [Description("User's access token for Graph API")] string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await CallCopilotChatApiAsync(query, accessToken, cancellationToken);
    }

    private async Task<string> CallCopilotChatApiAsync(
        string query,
        string accessToken,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calling Copilot Chat API with query: {Query}", query);

        try
        {
            // Create Kiota client with user's access token
            var client = CreateCopilotClient(accessToken);

            // Step 1: Create conversation
            _logger.LogInformation("Creating conversation...");
            var conversation = await client.Copilot.Conversations.PostAsync(
                new AgentOrchestrator.CopilotSdk.Copilot.Conversations.ConversationsPostRequestBody(),
                cancellationToken: cancellationToken);

            if (conversation?.Id == null)
            {
                throw new InvalidOperationException("Failed to create conversation - no ID returned");
            }

            _logger.LogInformation("Created conversation: {ConversationId}", conversation.Id);

            // Step 2: Send chat message
            var chatRequest = new CopilotChatRequest
            {
                Message = new CopilotMessageParameter { Text = query },
                LocationHint = new CopilotLocationHint { TimeZone = "UTC" }
            };

            _logger.LogInformation("Sending chat message...");
            var response = await client.Copilot.Conversations[conversation.Id.Value]
                .Chat.PostAsync(chatRequest, cancellationToken: cancellationToken);

            if (response?.Messages == null || response.Messages.Count == 0)
            {
                _logger.LogWarning("No messages in response");
                return "No response received from Copilot.";
            }

            // Get the assistant's response (last message)
            var assistantMessage = response.Messages.LastOrDefault();
            var responseText = assistantMessage?.Text ?? "No response content.";

            _logger.LogInformation("Received Copilot response");
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Copilot Chat API");
            throw;
        }
    }

    private CopilotApi CreateCopilotClient(string accessToken)
    {
        // Create HTTP client
        var httpClient = _httpClientFactory.CreateClient("Graph");

        // Create authentication provider with user's token
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new TokenProvider(accessToken));

        // Create request adapter (handles serialization internally)
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);

        return new CopilotApi(adapter);
    }
}

/// <summary>
/// Simple token provider that returns a pre-obtained access token
/// </summary>
internal class TokenProvider : IAccessTokenProvider
{
    private readonly string _accessToken;

    public TokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public AllowedHostsValidator AllowedHostsValidator => new();

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_accessToken);
    }
}
