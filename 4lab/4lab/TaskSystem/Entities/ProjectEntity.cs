namespace TaskSystem.Entities;

public class ProjectEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<JobEntity> Job { get; set; } = [];
}
