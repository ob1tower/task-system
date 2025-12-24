using CSharpFunctionalExtensions;
using TaskSystem.Dtos.Job;
using TaskSystem.RabbitMq.Messages;

namespace TaskSystem.Services.Jobs.Interfaces;

public interface IMessageBasedJobService
{
    Task<Result<Guid>> CreateJobAsync(JobCreateDto jobCreateDto);
    Task<Result> DeleteJobAsync(Guid id);
    Task<Result<List<JobGetDto>>> GetAllJobsAsync(int pageNumber, int pageSize);
    Task<Result<JobGetDto>> GetJobAsync(Guid id);
    Task<Result<Guid>> UpdateJobAsync(Guid id, JobUpdateDto jobUpdateDto);
    Task ProcessJobMessageAsync(JobMessageBase message);
}