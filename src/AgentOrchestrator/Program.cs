using AgentOrchestrator.Agent;
using AgentOrchestrator.Auth;
using AgentOrchestrator.Models;
using AgentOrchestrator.Plugins;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// === Load Configuration ===
var azureAdSettings = builder.Configuration.GetSection("AzureAd").Get<AzureAdSettings>()
    ?? throw new InvalidOperationException("AzureAd configuration is required");

var azureOpenAISettings = builder.Configuration.GetSection("AzureOpenAI").Get<AzureOpenAISettings>()
    ?? throw new InvalidOperationException("AzureOpenAI configuration is required");

var graphSettings = builder.Configuration.GetSection("MicrosoftGraph").Get<MicrosoftGraphSettings>()
    ?? throw new InvalidOperationException("MicrosoftGraph configuration is required");

var orchestrationSettings = builder.Configuration.GetSection("Orchestration").Get<OrchestrationSettings>()
    ?? new OrchestrationSettings();

// Register configuration as singletons
builder.Services.AddSingleton(azureAdSettings);
builder.Services.AddSingleton(azureOpenAISettings);
builder.Services.AddSingleton(graphSettings);
builder.Services.AddSingleton(orchestrationSettings);

// === Session Support (for web auth) ===
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();

// === Auth Services ===
builder.Services.AddSingleton<ITokenService, TokenService>();

// === HTTP Client for Graph API ===
builder.Services.AddHttpClient("Graph");

// === Semantic Kernel Setup ===
builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: azureOpenAISettings.DeploymentName,
        endpoint: azureOpenAISettings.Endpoint,
        apiKey: azureOpenAISettings.ApiKey
    );

    // Build kernel
    var kernel = kernelBuilder.Build();

    // Register plugins
    kernel.Plugins.AddFromObject(
        new IntentPlugin(kernel),
        "IntentPlugin");

    kernel.Plugins.AddFromObject(
        new AzureOpenAIPlugin(kernel),
        "AzureOpenAIPlugin");

    kernel.Plugins.AddFromObject(
        new M365CopilotPlugin(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<MicrosoftGraphSettings>(),
            sp.GetRequiredService<ILogger<M365CopilotPlugin>>()),
        "M365CopilotPlugin");

    kernel.Plugins.AddFromObject(
        new SynthesisPlugin(kernel),
        "SynthesisPlugin");

    return kernel;
});

// === M365 Agents SDK Setup ===
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Add AgentApplicationOptions from configuration
builder.AddAgentApplicationOptions();

// Register the agent
builder.AddAgent<OrchestratorAgent>();

// === CORS for development ===
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// === Middleware Pipeline ===
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSession();
app.UseAuthMiddleware();

app.UseCors();

// === Auth Endpoints (for web channel) ===
app.MapAuthEndpoints();

// === Agent Endpoint (M365 Agents SDK) ===
app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
});

// === Fallback to index.html for SPA ===
app.MapFallbackToFile("index.html");

app.Run();
