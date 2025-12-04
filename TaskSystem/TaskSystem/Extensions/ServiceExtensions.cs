using Asp.Versioning;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TaskSystem.DataAccess;
using TaskSystem.Filters;
using TaskSystem.Repositories;
using TaskSystem.Repositories.Interfaces;
using TaskSystem.Repositories.Jobs;
using TaskSystem.Repositories.Jobs.Interfaces;
using TaskSystem.Repositories.Projects;
using TaskSystem.Repositories.Projects.Interfaces;
using TaskSystem.Services;
using TaskSystem.Services.Interfaces;
using TaskSystem.Services.Jobs;
using TaskSystem.Services.Jobs.Interfaces;
using TaskSystem.Services.Projects;
using TaskSystem.Services.Projects.Interfaces;
using TaskSystem.Services.Shaper;
using TaskSystem.Services.Shaper.Interfaces;

namespace TaskSystem.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFixedWindowRateLimiter();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "TaskSystem";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
            configuration.GetConnectionString("Redis")!));

        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IUsersService, UsersService>();

        services.AddTransient<IPasswordHasher, PasswordHasher>();
        services.AddTransient<ITokensService, TokensService>();

        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));

        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UserDbContext")));

        services.AddDbContext<JobProjectDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("JobProjectDbContext")));

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IProjectService, ProjectService>();

        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<IdempotencyKeySwaggerFilter>();
        })
        .AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1);
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader());
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.ConfigureOptions<SwaggerVersionConfigurator>();

        services.AddIdempotentAPI(new IdempotencyOptions());
        services.AddIdempotentAPIUsingDistributedCache();

        services.AddScoped(typeof(IDataShaper<>), typeof(DataShaper<>));

        return services;
    }
}