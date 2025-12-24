using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskSystem.BackgroundServices;
using TaskSystem.DataAccess;
using TaskSystem.Extensions;
using TaskSystem.Middleware;
using TaskSystem.Services;
using TaskSystem.Services.Jobs;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/task-system-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

// Register idempotency service
builder.Services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();

// Register message processing service with improved retry mechanism
builder.Services.AddScoped<IMessageProcessingService, MessageProcessingService>();

// Register the hosted service for consuming messages
builder.Services.AddHostedService<JobConsumerHostedService>();

var app = builder.Build();

// Apply database migrations
try
{
    using (var scope = app.Services.CreateScope())
    {
        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var jobContext = scope.ServiceProvider.GetRequiredService<JobProjectDbContext>();

        // Apply migrations with a retry in case PostgreSQL is not ready yet
        var retryCount = 0;
        const int maxRetries = 10;
        while (retryCount < maxRetries)
        {
            try
            {
                userContext.Database.Migrate();
                jobContext.Database.Migrate();
                break;
            }
            catch (Npgsql.NpgsqlException ex) when (ex.Message.Contains("does not exist"))
            {
                // If database doesn't exist, wait a bit and retry
                Console.WriteLine($"Database not ready, waiting... (attempt {retryCount + 1}/{maxRetries})");
                System.Threading.Thread.Sleep(5000);
                retryCount++;
            }
        }

        if (retryCount >= maxRetries)
        {
            throw new Exception("Could not connect to database after multiple retries");
        }
    }
}
catch (Exception ex)
{
    Log.Logger.Error(ex, "An error occurred while migrating the database");
    throw;
}

// Global request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseSwaggerSetup();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();