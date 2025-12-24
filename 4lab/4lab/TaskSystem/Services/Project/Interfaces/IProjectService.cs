using CSharpFunctionalExtensions;
using TaskSystem.Dtos.Project;

namespace TaskSystem.Services.Projects.Interfaces;

public interface IProjectService
{
    Task<Result<Guid>> CreateProjects(ProjectCreateDto projectCreateDto);
    Task<Result> DeleteProjects(Guid id);
    Task<Result<ProjectGetDto>> GetProject(Guid id);
    Task<Result<List<ProjectGetDto>>> GetAllProjects(int pageNumber, int pageSize);
    Task<Result<Guid>> UpdateProjects(Guid id, ProjectUpdateDto projectUpdateDto);
}