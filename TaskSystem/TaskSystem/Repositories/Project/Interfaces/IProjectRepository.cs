using TaskSystem.Models;

namespace TaskSystem.Repositories.Projects.Interfaces
{
    public interface IProjectRepository
    {
        Task<Guid> CreateProjects(Project projects);
        Task<Guid> DeleteProjects(Guid id);
        Task<Project> GetProject(Guid id);
        Task<List<Project>> GetProjects(int pageNumber, int pageSize);
        Task<Guid> UpdateProjects(Guid projectId, string name, string? description);
        Task<Project> SearchTitle(string name);
    }
}