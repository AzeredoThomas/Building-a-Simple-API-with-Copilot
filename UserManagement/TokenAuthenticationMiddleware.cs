using System.Text.Json;

namespace UserManagement;

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly string _validToken;

    public TokenAuthenticationMiddleware(RequestDelegate next, ILogger<TokenAuthenticationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _validToken = configuration["Authentication:Token"]; // Store your valid token securely
    }

    public async Task Invoke(HttpContext context)
    {
        // Retrieve Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Authorization header missing or malformed.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized: Token missing or malformed." }));
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        if (token != _validToken)
        {
            _logger.LogWarning("Invalid token received.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized: Invalid token." }));
            return;
        }

        await _next(context); // Token is valid, continue request
    }
}
