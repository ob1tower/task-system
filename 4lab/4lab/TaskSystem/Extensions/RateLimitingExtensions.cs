using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using TaskSystem.Dtos;
using System.Net;

namespace TaskSystem.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFixedWindowRateLimiter(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(context =>
                {
                    string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    return RateLimitPartition.GetFixedWindowLimiter(
                        userId
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? context.Request.Headers.Host.ToString(),
                        _ => new FixedWindowRateLimiterOptions()
                        {
                            PermitLimit = 10,       
                            QueueLimit = 0,         
                            Window = TimeSpan.FromSeconds(30), 
                            AutoReplenishment = true,
                        });
                });

            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                    context.HttpContext.Response.Headers.Append("X-Limit-Remaining", "0");
                }

                var response = new ExceptionResponse(
                    StatusCode: HttpStatusCode.TooManyRequests,
                    Description: "Вы превысили лимит запросов. Попробуйте позже."
                );

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(response);
            };
        });

        return services;
    }
}