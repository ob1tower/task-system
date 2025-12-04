using TaskSystem.Models;

namespace TaskSystem.Repositories.Jobs.Interfaces
{
    public interface IJobRepository
    {
        Task<Guid> CreateJobs(Job jobs);
        Task<Guid> DeleteJobs(Guid id);
        Task<List<Job>> GetAllJobs(int pageNumber, int pageSize);
        Task<Job> GetJob(Guid id);
        Task<Guid> UpdateJobs(Guid jobId, string title, string? description, DateTime dueDate, Guid projectId);
        Task<Guid?> SearchProjectId(Guid id);
        Task<Job> SearchTitle(string title);
    }
}