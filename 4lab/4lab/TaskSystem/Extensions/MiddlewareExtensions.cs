using TaskSystem.Middleware;

namespace Microsoft.Extensions.DependencyInjection;

public static class MiddlewareExtensions
{
    public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}