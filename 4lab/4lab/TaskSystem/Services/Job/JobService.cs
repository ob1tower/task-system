using CSharpFunctionalExtensions;
using TaskSystem.Dtos.Job;
using TaskSystem.Models;
using TaskSystem.Repositories.Jobs.Interfaces;
using TaskSystem.Services.Jobs.Interfaces;

namespace TaskSystem.Services.Jobs;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;

    public JobService(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<Result<List<JobGetDto>>> GetAllJobs(int pageNumber, int pageSize)
    {

        var jobs = await _jobRepository.GetAllJobs(pageNumber, pageSize);

        var result = jobs.Select(job => new JobGetDto(job.JobId, job.Title, job.Description,
                                                      job.DueDate, job.CreatedAt)).ToList();

        return Result.Success(result);
    }

    public async Task<Result<JobGetDto>> GetJob(Guid id)
    {
        var jobs = await _jobRepository.GetJob(id);

        if (jobs == null)
        {
            return Result.Failure<JobGetDto>($"Job with Id {id} not found.");
        }

        var result = new JobGetDto(jobs.JobId, jobs.Title, jobs.Description,
                                   jobs.DueDate, jobs.CreatedAt);

        return Result.Success(result);
    }

    public async Task<Result<Guid>> CreateJobs(JobCreateDto jobCreateDto)
    {
        var projectId = await _jobRepository.SearchProjectId(jobCreateDto.ProjectId);

        if (projectId == null)
        {
            return Result.Failure<Guid>($"Project with Id {jobCreateDto.ProjectId} not found.");
        }

        var title = await _jobRepository.SearchTitle(jobCreateDto.Title);

        if (title != null)
        {
            return Result.Failure<Guid>($"Job with title '{jobCreateDto.Title}' already exists.");
        }

        var job = new Job(Guid.NewGuid(), jobCreateDto.Title, null,
                          DateTime.UtcNow, jobCreateDto.ProjectId);

        var createJob = await _jobRepository.CreateJobs(job);

        return Result.Success(createJob); 
    }

    public async Task<Result<Guid>> UpdateJobs(Guid id, JobUpdateDto jobUpdateDto)
    {
        var projectId = await _jobRepository.SearchProjectId(jobUpdateDto.ProjectId);
        if (projectId == null)
        {
            return Result.Failure<Guid>($"Project with Id {jobUpdateDto.ProjectId} not found.");
        }

        var title = await _jobRepository.SearchTitle(jobUpdateDto.Title);
        if (title != null && title.JobId != id)
        {
            return Result.Failure<Guid>($"Job with title '{jobUpdateDto.Title}' already exists.");
        }

        var job = new Job(id, jobUpdateDto.Title, jobUpdateDto.Description,
                          jobUpdateDto.DueDate, jobUpdateDto.ProjectId);

        var updatedJobId = await _jobRepository.UpdateJobs(id, jobUpdateDto.Title, jobUpdateDto.Description,
                                                           jobUpdateDto.DueDate, jobUpdateDto.ProjectId);

        return Result.Success(updatedJobId);
    }

    public async Task<Result> DeleteJobs(Guid id)
    {
        var jobId = await _jobRepository.GetJob(id);

        if (jobId == null)
        {
            return Result.Failure($"Job with Id {id} not found.");
        }

        var deletedJobId = await _jobRepository.DeleteJobs(id);

        return Result.Success(deletedJobId);
    }
}