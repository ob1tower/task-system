using System.Net;
using TaskSystem.Dtos;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {   
        ExceptionResponse response = exception switch
        {
            ApplicationException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Произошло исключение из приложения."),
            KeyNotFoundException _ => new ExceptionResponse(HttpStatusCode.NotFound, "Ресурс не найден."),
            UnauthorizedAccessException _ => new ExceptionResponse(HttpStatusCode.Unauthorized, "Неавторизованный."),
            _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера. Пожалуйста, повторите попытку позже.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)response.StatusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}