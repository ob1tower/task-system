using TaskSystemClient.Models;
using TaskSystemClient.Models.Job;
using TaskSystemClient.Models.Project;
using TaskSystemClient.Models.User;

namespace TaskSystemClient.Services;

public interface IMessageService : IDisposable
{
    // Authentication operations
    Task<RegisterResponseMessage?> RegisterAsync(UserRegisterDto registerDto, CancellationToken cancellationToken = default);
    Task<LoginResponseMessage?> LoginAsync(UserLoginDto loginDto, CancellationToken cancellationToken = default);

    // Job operations
    Task SendCreateJobAsync(JobCreateDto jobCreateDto, CancellationToken cancellationToken = default);
    Task SendUpdateJobAsync(Guid jobId, JobUpdateDto jobUpdateDto, CancellationToken cancellationToken = default);
    Task SendDeleteJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    // Methods that return data
    Task<string?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<string?> GetAllJobsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Project operations
    Task SendCreateProjectAsync(ProjectCreateDto projectCreateDto, CancellationToken cancellationToken = default);
    Task SendUpdateProjectAsync(Guid projectId, ProjectUpdateDto projectUpdateDto, CancellationToken cancellationToken = default);
    Task SendDeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    // Methods that return data
    Task<string?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<string?> GetAllProjectsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}