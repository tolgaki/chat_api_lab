using Microsoft.Identity.Client;
using AgentOrchestrator.Models;

namespace AgentOrchestrator.Auth;

public interface ITokenService
{
    Task<string> GetAccessTokenAsync(string sessionId);
    Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string code, string sessionId);
    void ClearTokenCache(string sessionId);
    string BuildAuthorizationUrl(string state);
}

public class TokenService : ITokenService
{
    private readonly IConfidentialClientApplication _msalClient;
    private readonly AzureAdSettings _settings;
    private readonly ILogger<TokenService> _logger;
    private readonly Dictionary<string, AuthenticationResult> _tokenCache = new();
    private readonly object _cacheLock = new();

    public TokenService(AzureAdSettings settings, ILogger<TokenService> logger)
    {
        _settings = settings;
        _logger = logger;

        _msalClient = ConfidentialClientApplicationBuilder
            .Create(settings.ClientId)
            .WithClientSecret(settings.ClientSecret)
            .WithAuthority($"{settings.Instance}{settings.TenantId}")
            .WithRedirectUri($"http://localhost:5000{settings.CallbackPath}")
            .Build();
    }

    public string BuildAuthorizationUrl(string state)
    {
        var scopes = _settings.Scopes;

        var authUrl = _msalClient
            .GetAuthorizationRequestUrl(scopes)
            .WithExtraQueryParameters($"state={Uri.EscapeDataString(state)}")
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        return authUrl.ToString();
    }

    public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string code, string sessionId)
    {
        var scopes = _settings.Scopes;

        var result = await _msalClient
            .AcquireTokenByAuthorizationCode(scopes, code)
            .ExecuteAsync();

        lock (_cacheLock)
        {
            _tokenCache[sessionId] = result;
        }

        _logger.LogInformation("Token acquired for session {SessionId}", sessionId);
        return result;
    }

    public async Task<string> GetAccessTokenAsync(string sessionId)
    {
        AuthenticationResult? cachedResult;

        lock (_cacheLock)
        {
            _tokenCache.TryGetValue(sessionId, out cachedResult);
        }

        if (cachedResult == null)
        {
            throw new InvalidOperationException("No token found for session. Please login first.");
        }

        // Check if token is expired or about to expire (within 5 minutes)
        if (cachedResult.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation("Token expired or expiring soon, attempting refresh for session {SessionId}", sessionId);

            try
            {
                var accounts = await _msalClient.GetAccountsAsync();
                var account = accounts.FirstOrDefault();

                if (account != null)
                {
                    var result = await _msalClient
                        .AcquireTokenSilent(_settings.Scopes, account)
                        .ExecuteAsync();

                    lock (_cacheLock)
                    {
                        _tokenCache[sessionId] = result;
                    }

                    return result.AccessToken;
                }
            }
            catch (MsalUiRequiredException)
            {
                _logger.LogWarning("Silent token acquisition failed, user needs to re-authenticate");
                throw new InvalidOperationException("Session expired. Please login again.");
            }
        }

        return cachedResult.AccessToken;
    }

    public void ClearTokenCache(string sessionId)
    {
        lock (_cacheLock)
        {
            _tokenCache.Remove(sessionId);
        }

        _logger.LogInformation("Token cache cleared for session {SessionId}", sessionId);
    }
}
