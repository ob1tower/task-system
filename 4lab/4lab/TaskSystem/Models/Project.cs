namespace TaskSystem.Models;
public class Project
{
    public Guid ProjectId { get; }
    public string Name { get; } = string.Empty;
    public string? Description { get; }
    public DateTime CreatedAt { get; }

    public List<Job> Jobs { get; set; } = [];

    public Project(Guid projectId, string name, string? description)
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}
