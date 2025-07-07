namespace UserManagement;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Log Request details
        _logger.LogInformation("HTTP Request: {method} {url}", context.Request.Method, context.Request.Path);

        // Copy original response body stream
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context); // Invoke next middleware

        // Log Response details
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation("HTTP Response: {statusCode} {body}", context.Response.StatusCode, responseText);

        await responseBody.CopyToAsync(originalBodyStream);
    }
}
