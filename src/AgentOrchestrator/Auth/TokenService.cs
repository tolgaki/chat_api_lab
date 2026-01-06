using System.Collections.Concurrent;
using Microsoft.Identity.Client;
using AgentOrchestrator.Models;

namespace AgentOrchestrator.Auth;

public interface ITokenService
{
    Task<string> GetAccessTokenAsync(string sessionId);
    Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string code, string sessionId);
    void ClearTokenCache(string sessionId);
    Task<string> BuildAuthorizationUrlAsync(string state);
}

public class TokenService : ITokenService
{
    private readonly IConfidentialClientApplication _msalClient;
    private readonly AzureAdSettings _settings;
    private readonly ILogger<TokenService> _logger;

    // Thread-safe caches
    // TODO: For production, implement distributed cache:
    // - Redis: Use IDistributedCache with StackExchangeRedis
    // - MSAL serialization: _msalClient.UserTokenCache.SetBeforeAccess/SetAfterAccess
    private readonly ConcurrentDictionary<string, AuthenticationResult> _tokenCache = new();
    private readonly ConcurrentDictionary<string, string> _accountIdentifiers = new();

    public TokenService(AzureAdSettings settings, ILogger<TokenService> logger)
    {
        _settings = settings;
        _logger = logger;

        _msalClient = ConfidentialClientApplicationBuilder
            .Create(settings.ClientId)
            .WithClientSecret(settings.ClientSecret)
            .WithAuthority($"{settings.Instance}{settings.TenantId}")
            .WithRedirectUri(settings.RedirectUri)
            .Build();
    }

    public async Task<string> BuildAuthorizationUrlAsync(string state)
    {
        var scopes = _settings.Scopes;

        var authUrl = await _msalClient
            .GetAuthorizationRequestUrl(scopes)
            .WithExtraQueryParameters($"state={Uri.EscapeDataString(state)}")
            .ExecuteAsync();

        return authUrl.ToString();
    }

    public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string code, string sessionId)
    {
        var scopes = _settings.Scopes;

        var result = await _msalClient
            .AcquireTokenByAuthorizationCode(scopes, code)
            .ExecuteAsync();

        _tokenCache[sessionId] = result;

        // Store account identifier for proper token refresh (fixes deprecated GetAccountsAsync)
        if (result.Account?.HomeAccountId?.Identifier != null)
        {
            _accountIdentifiers[sessionId] = result.Account.HomeAccountId.Identifier;
        }

        _logger.LogInformation("Token acquired for session {SessionId}", sessionId);
        return result;
    }

    public async Task<string> GetAccessTokenAsync(string sessionId)
    {
        if (!_tokenCache.TryGetValue(sessionId, out var cachedResult))
        {
            throw new InvalidOperationException("No token found for session. Please login first.");
        }

        // Check if token is expired or about to expire (within 5 minutes)
        if (cachedResult.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation("Token expired or expiring soon, attempting refresh for session {SessionId}", sessionId);

            try
            {
                // Use stored account identifier instead of deprecated GetAccountsAsync()
                if (_accountIdentifiers.TryGetValue(sessionId, out var accountIdentifier))
                {
                    var account = await _msalClient.GetAccountAsync(accountIdentifier);

                    if (account != null)
                    {
                        var result = await _msalClient
                            .AcquireTokenSilent(_settings.Scopes, account)
                            .ExecuteAsync();

                        _tokenCache[sessionId] = result;
                        return result.AccessToken;
                    }
                }

                _logger.LogWarning("No account identifier found for session {SessionId}", sessionId);
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
        _tokenCache.TryRemove(sessionId, out _);
        _accountIdentifiers.TryRemove(sessionId, out _);

        _logger.LogInformation("Token cache cleared for session {SessionId}", sessionId);
    }
}
