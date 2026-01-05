namespace AgentOrchestrator.Auth;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;

    private static readonly string[] PublicPaths =
    [
        "/auth/login",
        "/auth/callback",
        "/auth/status",
        "/",
        "/index.html",
        "/css",
        "/js"
    ];

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Allow public paths
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Check authentication for API endpoints
        var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";

        if (!isAuthenticated)
        {
            _logger.LogWarning("Unauthorized access attempt to {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized. Please login first." });
            return;
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        foreach (var publicPath in PublicPaths)
        {
            if (path.Equals(publicPath, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(publicPath + "/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(publicPath + ".", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public static class AuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthMiddleware>();
    }
}
