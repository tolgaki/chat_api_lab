using AgentOrchestrator.Auth;
using AgentOrchestrator.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgentOrchestrator.Tests.Auth;

public class TokenServiceTests
{
    private readonly AzureAdSettings _settings;
    private readonly Mock<ILogger<TokenService>> _loggerMock;

    public TokenServiceTests()
    {
        _settings = new AzureAdSettings
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUri = "http://localhost:5000/auth/callback",
            Scopes = ["openid", "profile", "User.Read"]
        };
        _loggerMock = new Mock<ILogger<TokenService>>();
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldCreateInstance()
    {
        // Act
        var tokenService = new TokenService(_settings, _loggerMock.Object);

        // Assert
        Assert.NotNull(tokenService);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithNoTokenCached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tokenService = new TokenService(_settings, _loggerMock.Object);
        var sessionId = "test-session-id";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => tokenService.GetAccessTokenAsync(sessionId));
    }

    [Fact]
    public void ClearTokenCache_WithValidSessionId_ShouldNotThrow()
    {
        // Arrange
        var tokenService = new TokenService(_settings, _loggerMock.Object);
        var sessionId = "test-session-id";

        // Act & Assert - should not throw even if session doesn't exist
        var exception = Record.Exception(() => tokenService.ClearTokenCache(sessionId));
        Assert.Null(exception);
    }
}
