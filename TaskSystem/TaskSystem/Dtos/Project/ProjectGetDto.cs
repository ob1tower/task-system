namespace TaskSystem.Dtos.Project;

public record ProjectGetDto(Guid ProjectId, string Name, 
                            string? Description, DateTime CreatedAt);