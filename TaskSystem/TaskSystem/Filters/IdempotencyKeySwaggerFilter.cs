using IdempotentAPI.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskSystem.Filters;

public class IdempotencyKeySwaggerFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        if (!context.MethodInfo.GetCustomAttributes(true)
            .OfType<IdempotentAttribute>()
            .Any())
            return;

        operation.Parameters.Add(new OpenApiParameter()
        {
            Name = "IdempotencyKey",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema()
            {
                Type = "string",
                Format = "uuid",
            },
        });
    }
}
