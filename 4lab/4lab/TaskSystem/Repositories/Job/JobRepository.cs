using Microsoft.EntityFrameworkCore;
using TaskSystem.DataAccess;
using TaskSystem.Entities;
using TaskSystem.Models;
using TaskSystem.Repositories.Jobs.Interfaces;

namespace TaskSystem.Repositories.Jobs;

public class JobRepository : IJobRepository
{
    private readonly JobProjectDbContext _context;

    public JobRepository(JobProjectDbContext context)
    {
        _context = context;
    }

    public async Task<List<Job>> GetAllJobs(int pageNumber, int pageSize)
    {
        var jobEntities = await _context.Jobs
            .Include(n => n.Project)
            .AsNoTracking()
            .OrderBy(j => j.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var jobs = jobEntities
            .Select(b => new Job(b.JobId, b.Title, b.Description, 
                                 b.DueDate, b.ProjectId)).ToList();

        return jobs;
    }

    public async Task<Job> GetJob(Guid id)
    {
        var jobEntities = await _context.Jobs
            .Include(n => n.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.JobId == id);

        if (jobEntities == null)
            return null!;

        var jobs = new Job(jobEntities.JobId, jobEntities.Title, jobEntities.Description,
                             jobEntities.DueDate, jobEntities.ProjectId);

        return jobs;
    }

    public async Task<Guid> CreateJobs(Job jobs)
    {
        var jobEntities = new JobEntity
        {
            JobId = jobs.JobId,
            Title = jobs.Title,
            Description = jobs.Description,
            DueDate = jobs.DueDate,
            ProjectId = jobs.ProjectId,
            CreatedAt = DateTime.UtcNow,
        };

        await _context.Jobs.AddAsync(jobEntities);
        await _context.SaveChangesAsync();
        return jobEntities.JobId;
    }

    public async Task<Guid> UpdateJobs(Guid jobId, string title, string? description, DateTime dueDate, Guid projectId)
    {
        await _context.Jobs
            .Where(b => b.JobId == jobId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Title, b => title)
                .SetProperty(b => b.Description, b => description)
                .SetProperty(b => b.DueDate, b => dueDate)
                .SetProperty(b => b.ProjectId, b => projectId));

        return jobId;
    }

    public async Task<Guid> DeleteJobs(Guid id)
    {
        await _context.Jobs
            .Where(b => b.JobId == id)
            .ExecuteDeleteAsync();

        return id;
    }

    public async Task<Guid?> SearchProjectId(Guid id)
    {
        var project = await _context.Projects
            .AsNoTracking() 
            .FirstOrDefaultAsync(p => p.ProjectId == id);

        if (project == null)
            return null!;

        return project.ProjectId;
    }

    public async Task<Job> SearchTitle(string title)
    {
        var jobEntities = await _context.Jobs
        .AsNoTracking()
        .FirstOrDefaultAsync(job => job.Title == title);

        if (jobEntities == null)
            return null!;

        var jobs = new Job(jobEntities.JobId, jobEntities.Title, jobEntities.Description, 
                           jobEntities.DueDate, jobEntities.ProjectId);

        return jobs;
    }
}