namespace TaskSystem.Dtos.Job;

public record JobGetDto(Guid JobId, string Title, string? Description, 
                        DateTime DueDate, DateTime CreatedAt);