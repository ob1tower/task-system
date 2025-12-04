using Microsoft.EntityFrameworkCore;
using TaskSystem.DataAccess;
using TaskSystem.Entities;
using TaskSystem.Models;
using TaskSystem.Repositories.Projects.Interfaces;

namespace TaskSystem.Repositories.Projects;

public class ProjectRepository : IProjectRepository
{
    private readonly JobProjectDbContext _context;

    public ProjectRepository(JobProjectDbContext context)
    {
        _context = context;
    }

    public async Task<List<Project>> GetProjects(int pageNumber, int pageSize)
    {
        var projectEntities = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Job)
            .OrderBy(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var projects = projectEntities
            .Select(b => new Project(b.ProjectId, b.Name, b.Description))
            .ToList();

        return projects;
    }

    public async Task<Project> GetProject(Guid id)
    {
        var projectEntities = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ProjectId == id);

        if (projectEntities == null)
            return null!;

        var projects = new Project(projectEntities.ProjectId, projectEntities.Name,
                                   projectEntities.Description);

        return projects;
    }

    public async Task<Guid> CreateProjects(Project projects)
    {
        var projectEntities = new ProjectEntity
        {
            ProjectId = projects.ProjectId,
            Name = projects.Name,
            Description = projects.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(projectEntities);
        await _context.SaveChangesAsync();
        return projectEntities.ProjectId;
    }

    public async Task<Guid> UpdateProjects(Guid projectId, string name, string? description)
    {
        await _context.Projects
           .Where(b => b.ProjectId == projectId)
           .ExecuteUpdateAsync(s => s
               .SetProperty(b => b.Name, b => name)
               .SetProperty(b => b.Description, b => description));

        return projectId;
    }

    public async Task<Guid> DeleteProjects(Guid id)
    {
        await _context.Projects
            .Where(b => b.ProjectId == id)
            .ExecuteDeleteAsync();

        return id;
    }

    public async Task<Project> SearchTitle(string name)
    {
        var projectEntities = await _context.Projects
        .AsNoTracking()
        .FirstOrDefaultAsync(b => b.Name == name);

        if (projectEntities == null)
            return null!;

        var projects = new Project(projectEntities.ProjectId, projectEntities.Name,
                                   projectEntities.Description)
        {
            Jobs = projectEntities.Job.Select(n => new Job(n.JobId, n.Title, n.Description, 
                                                           n.DueDate, n.ProjectId)).ToList()
        };

        return projects;
    }
}
