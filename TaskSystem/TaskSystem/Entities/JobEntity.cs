namespace TaskSystem.Entities;

public class JobEntity
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
}
