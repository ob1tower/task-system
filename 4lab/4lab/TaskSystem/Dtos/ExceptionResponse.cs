using System.Net;

namespace TaskSystem.Dtos;

public record ExceptionResponse(HttpStatusCode StatusCode, string Description);