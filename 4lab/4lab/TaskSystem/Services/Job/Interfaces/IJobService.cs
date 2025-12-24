using CSharpFunctionalExtensions;
using TaskSystem.Dtos.Job;

namespace TaskSystem.Services.Jobs.Interfaces;
public interface IJobService
{
    Task<Result<Guid>> CreateJobs(JobCreateDto jobCreateDto);
    Task<Result> DeleteJobs(Guid id);
    Task<Result<List<JobGetDto>>> GetAllJobs(int pageNumber, int pageSize);
    Task<Result<JobGetDto>> GetJob(Guid id);
    Task<Result<Guid>> UpdateJobs(Guid id, JobUpdateDto jobUpdateDto);
}