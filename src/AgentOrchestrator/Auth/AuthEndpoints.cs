using Microsoft.AspNetCore.Mvc;

namespace AgentOrchestrator.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapGet("/login", Login);
        group.MapGet("/callback", Callback);
        group.MapPost("/logout", Logout);
        group.MapGet("/status", Status);
    }

    private static IResult Login(
        HttpContext context,
        ITokenService tokenService)
    {
        // Generate or get session ID
        var sessionId = context.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Session.SetString("SessionId", sessionId);
        }

        // Build authorization URL with state parameter for CSRF protection
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        context.Session.SetString("AuthState", state);

        var authUrl = tokenService.BuildAuthorizationUrl(state);

        return Results.Redirect(authUrl);
    }

    private static async Task<IResult> Callback(
        HttpContext context,
        ITokenService tokenService,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        // Handle error from identity provider
        if (!string.IsNullOrEmpty(error))
        {
            return Results.BadRequest(new { error, description = errorDescription });
        }

        // Validate code
        if (string.IsNullOrEmpty(code))
        {
            return Results.BadRequest(new { error = "No authorization code received" });
        }

        // Validate state for CSRF protection
        var expectedState = context.Session.GetString("AuthState");
        if (state != expectedState)
        {
            return Results.BadRequest(new { error = "Invalid state parameter" });
        }

        var sessionId = context.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Session.SetString("SessionId", sessionId);
        }

        try
        {
            var authResult = await tokenService.AcquireTokenByAuthorizationCodeAsync(code, sessionId);

            // Store user info in session
            context.Session.SetString("UserName", authResult.Account?.Username ?? "Unknown");
            context.Session.SetString("IsAuthenticated", "true");

            // Clear auth state
            context.Session.Remove("AuthState");

            // Redirect to home page
            return Results.Redirect("/");
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Token acquisition failed", details = ex.Message });
        }
    }

    private static IResult Logout(
        HttpContext context,
        ITokenService tokenService)
    {
        var sessionId = context.Session.GetString("SessionId");
        if (!string.IsNullOrEmpty(sessionId))
        {
            tokenService.ClearTokenCache(sessionId);
        }

        context.Session.Clear();

        return Results.Ok(new { message = "Logged out successfully" });
    }

    private static IResult Status(HttpContext context)
    {
        var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";
        var userName = context.Session.GetString("UserName");

        return Results.Ok(new
        {
            isAuthenticated,
            userName = isAuthenticated ? userName : null
        });
    }
}
