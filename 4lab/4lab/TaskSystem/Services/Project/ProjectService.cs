using CSharpFunctionalExtensions;
using TaskSystem.Dtos.Project;
using TaskSystem.Models;
using TaskSystem.Repositories.Projects.Interfaces;
using TaskSystem.Services.Projects.Interfaces;

namespace TaskSystem.Services.Projects;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<List<ProjectGetDto>>> GetAllProjects(int pageNumber, int pageSize)
    {
        var project = await _projectRepository.GetProjects(pageNumber, pageSize);

        var result = project.Select(project => new ProjectGetDto(project.ProjectId, project.Name,
                                                                 project.Description, project.CreatedAt)).ToList();

        return Result.Success(result);
    }

    public async Task<Result<ProjectGetDto>> GetProject(Guid id)
    {
        var project = await _projectRepository.GetProject(id);

        if (project == null)
        {
            return Result.Failure<ProjectGetDto>($"Проект с Id {id} не найден.");
        }

        var result = new ProjectGetDto(project.ProjectId, project.Name, project.Description, project.CreatedAt);

        return Result.Success(result);
    }

    public async Task<Result<Guid>> CreateProjects(ProjectCreateDto projectCreateDto)
    {
        try
        {
            var name = await _projectRepository.SearchTitle(projectCreateDto.Name);

            if (name != null)
            {
                return Result.Failure<Guid>($"Проект с названием '{projectCreateDto.Name}' уже существует.");
            }

            var project = new Project(Guid.NewGuid(), projectCreateDto.Name, null);
            Console.WriteLine($"Creating project with ID: {project.ProjectId}, Name: {project.Name}");

            var createProject = await _projectRepository.CreateProjects(project);
            Console.WriteLine($"Project created with result: {createProject}");

            return Result.Success(createProject);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CreateProjects: {ex.Message}");
            return Result.Failure<Guid>($"Ошибка при создании проекта: {ex.Message}");
        }
    }

    public async Task<Result<Guid>> UpdateProjects(Guid id, ProjectUpdateDto projectUpdateDto)
    {
        var title = await _projectRepository.SearchTitle(projectUpdateDto.Name);
        if (title != null && title.ProjectId != id)
        {
            return Result.Failure<Guid>($"Проект с названием '{projectUpdateDto.Name}' уже существует.");
        }

        var project = new Project(id, projectUpdateDto.Name, projectUpdateDto.Description);


        var updatedProjectId = await _projectRepository.UpdateProjects(id, projectUpdateDto.Name, projectUpdateDto.Description);

        return Result.Success(updatedProjectId);

    }

    public async Task<Result> DeleteProjects(Guid id)
    {
        var projectId = await _projectRepository.GetProject(id);

        if (projectId == null)
        {
            return Result.Failure($"Проект с Id {id} не найдена.");
        }

        var deletedProjectId = await _projectRepository.DeleteProjects(id);

        return Result.Success(deletedProjectId);
    }
}
