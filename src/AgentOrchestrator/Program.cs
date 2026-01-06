using System.Threading.RateLimiting;
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
// TODO: For production deployment with multiple instances, replace with:
// - Redis: builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = "..."; });
// - SQL Server: builder.Services.AddDistributedSqlServerCache(options => { ... });
builder.Services.AddDistributedMemoryCache(); // Single-instance only
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

// === HTTP Client for Graph API with Resilience ===
builder.Services.AddHttpClient("Graph")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });

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

    // Get logger factory for plugin logging
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    // Register plugins with logging
    kernel.Plugins.AddFromObject(
        new IntentPlugin(kernel, loggerFactory.CreateLogger<IntentPlugin>()),
        "IntentPlugin");

    kernel.Plugins.AddFromObject(
        new AzureOpenAIPlugin(kernel, loggerFactory.CreateLogger<AzureOpenAIPlugin>()),
        "AzureOpenAIPlugin");

    kernel.Plugins.AddFromObject(
        new M365CopilotPlugin(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<MicrosoftGraphSettings>(),
            sp.GetRequiredService<ILogger<M365CopilotPlugin>>()),
        "M365CopilotPlugin");

    kernel.Plugins.AddFromObject(
        new SynthesisPlugin(kernel, loggerFactory.CreateLogger<SynthesisPlugin>()),
        "SynthesisPlugin");

    return kernel;
});

// === M365 Agents SDK Setup ===
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Add AgentApplicationOptions from configuration
builder.AddAgentApplicationOptions();

// Register the agent
builder.AddAgent<OrchestratorAgent>();

// === CORS Configuration ===
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5000"];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// === Rate Limiting ===
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 30,
                QueueLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

// === Health Checks ===
builder.Services.AddHealthChecks();

// === Swagger/OpenAPI ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Agent Orchestrator API",
        Version = "v1",
        Description = "API for the .NET 10 Agent Orchestrator with M365 Copilot integration"
    });
});

var app = builder.Build();

// === Middleware Pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Agent Orchestrator API v1");
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSession();
app.UseAuthMiddleware();

app.UseCors();
app.UseRateLimiter();

// === Health Check Endpoints ===
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

// === Auth Endpoints (for web channel) ===
app.MapAuthEndpoints();

// === Agent Endpoint (M365 Agents SDK) ===
app.MapPost("/api/messages", async (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
    await adapter.ProcessAsync(request, response, agent, cancellationToken);
}).RequireRateLimiting("api");

// === Fallback to index.html for SPA ===
app.MapFallbackToFile("index.html");

app.Run();
