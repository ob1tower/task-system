namespace TaskSystem.Dtos.Job;

public record JobUpdateDto(string Title, string? Description,
                           DateTime DueDate, Guid ProjectId);