namespace TaskSystem.Models;

public class Job
{
    public Guid JobId { get; }
    public string Title { get; } = string.Empty;
    public string? Description { get; }
    public DateTime DueDate { get; }
    public DateTime CreatedAt { get; }

    public Guid ProjectId { get; }
    public Project? Project { get; }

    public Job(Guid jobId, string title, string? description, DateTime dueDate, Guid projectId)
    {
        JobId = jobId;
        Title = title;
        Description = description;
        DueDate = dueDate;
        ProjectId = projectId;
        CreatedAt = DateTime.UtcNow;
    }
}
