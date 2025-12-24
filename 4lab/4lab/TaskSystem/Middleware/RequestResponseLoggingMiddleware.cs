using Microsoft.Extensions.Logging;

namespace TaskSystem.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the incoming request
        var request = context.Request;
        var response = context.Response;

        var originalBodyStream = response.Body;

        using var responseBody = new MemoryStream();
        response.Body = responseBody;

        var requestTime = DateTime.UtcNow;
        var requestId = context.TraceIdentifier;

        _logger.LogInformation(
            "Incoming Request: {Method} {Scheme}://{Host}{Path} {QueryString} - RequestId: {RequestId} - User: {UserId}",
            request.Method,
            request.Scheme,
            request.Host,
            request.Path,
            request.QueryString,
            requestId,
            context.User?.Identity?.Name ?? "Anonymous");

        try
        {
            await _next(context);
        }
        finally
        {
            // Log the response
            var responseTime = DateTime.UtcNow;
            var elapsedMs = (responseTime - requestTime).TotalMilliseconds;

            response.Body.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation(
                "Outgoing Response: {StatusCode} - RequestId: {RequestId} - Elapsed: {ElapsedMs}ms - User: {UserId}",
                response.StatusCode,
                requestId,
                elapsedMs,
                context.User?.Identity?.Name ?? "Anonymous");

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}