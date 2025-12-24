using TaskSystemClient.Models;
using TaskSystemClient.Models.Job;
using TaskSystemClient.Models.Project;
using TaskSystemClient.Models.User;
using TaskSystemClient.Services;

namespace TaskSystemClient;

public interface ICommandHandler
{
    void HandleCommand(string input);
    bool IsExitCommand { get; }
}

public class JobCommandHandler : ICommandHandler
{
    private readonly IMessageService _messageService;
    private readonly IAuthService _authService;
    public bool IsExitCommand { get; private set; }

    public JobCommandHandler(IMessageService messageService, IAuthService authService)
    {
        _messageService = messageService;
        _authService = authService;
    }

    public void HandleCommand(string input)
    {
        if (string.IsNullOrEmpty(input))
            return;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        var command = parts[0].ToLower();

        switch (command)
        {
            case "register":
                HandleRegisterCommand(parts);
                break;
            case "login":
                HandleLoginCommand(parts);
                break;
            case "logout":
                HandleLogoutCommand();
                break;
            case "create":
                // Check if user is authenticated before allowing job operations
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to perform job operations.");
                    return;
                }
                HandleCreateCommand(parts);
                break;
            case "update":
                // Check if user isauthenticated before allowing job operations
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to perform job operations.");
                    return;
                }
                HandleUpdateCommand(parts);
                break;
            case "delete":
                // Check if user is authenticated before allowing job operations
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to perform job operations.");
                    return;
                }
                HandleDeleteCommand(parts);
                break;
            case "get":
                // Check if user is authenticated before allowing job operations
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to perform job operations.");
                    return;
                }
                HandleGetCommand(parts);
                break;
            case "list":
                // Check if user is authenticated before allowing job operations
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to perform job operations.");
                    return;
                }
                HandleListCommand(parts);
                break;
            case "createproject":
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to create projects.");
                    return;
                }
                HandleCreateProjectCommand(parts);
                break;
            case "updateproject":
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to update projects.");
                    return;
                }
                HandleUpdateProjectCommand(parts);
                break;
            case "deleteproject":
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to delete projects.");
                    return;
                }
                HandleDeleteProjectCommand(parts);
                break;
            case "getproject":
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to get a project.");
                    return;
                }
                HandleGetProjectCommand(parts);
                break;
            case "listprojects":
                if (!IsAuthenticated())
                {
                    Console.WriteLine("Error: You must be logged in to list projects.");
                    return;
                }
                HandleListProjectsCommand(parts);
                break;
            case "exit":
                HandleExitCommand();
                break;
            default:
                Console.WriteLine("Unknown command. See available commands below.");
                ShowHelp();
                break;
        }
    }

    private bool IsAuthenticated()
    {
        return _authService.GetAccessToken() != null;
    }

    private async void HandleRegisterCommand(string[] parts)
    {
        if (parts.Length >= 4)
        {
            var userName = parts[1];
            var email = parts[2];
            var password = parts[3];

            var registerDto = new UserRegisterDto(userName, email, password);

            try
            {
                var response = await _messageService.RegisterAsync(registerDto);
                if (response != null)
                {
                    if (response.Success)
                    {
                        Console.WriteLine($"Registration successful for user: {userName}");
                    }
                    else
                    {
                        Console.WriteLine($"Registration failed: {response.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Registration request timed out for user: {userName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Usage: register <username> <email> <password>");
        }
    }

    private async void HandleLoginCommand(string[] parts)
    {
        if (parts.Length >= 3)
        {
            var email = parts[1];
            var password = parts[2];

            var loginDto = new UserLoginDto(email, password);

            try
            {
                var response = await _messageService.LoginAsync(loginDto);
                if (response != null)
                {
                    if (response.Success)
                    {
                        Console.WriteLine($"Login successful for email: {email}");
                        // Save token to AuthService
                        _authService.SetAccessToken(response.AccessToken);
                    }
                    else
                    {
                        Console.WriteLine($"Login failed: {response.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Login request timed out for email: {email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Usage: login <email> <password>");
        }
    }

    private void HandleLogoutCommand()
    {
        _authService.SetAccessToken(null);
        Console.WriteLine("Logged out successfully.");
    }

    private async void HandleCreateCommand(string[] parts)
    {
        if (parts.Length >= 3)
        {
            var title = parts[1];
            if (Guid.TryParse(parts[2], out var projectId))
            {
                var createDto = new JobCreateDto(title, projectId);
                await _messageService.SendCreateJobAsync(createDto);
                Console.WriteLine($"Create job request sent: {title}");
            }
            else
            {
                Console.WriteLine("Invalid project ID format");
            }
        }
        else
        {
            Console.WriteLine("Usage: create <title> <project_id>");
        }
    }

    private async void HandleUpdateCommand(string[] parts)
    {
        if (parts.Length >= 6)
        {
            if (Guid.TryParse(parts[1], out var jobId) &&
                Guid.TryParse(parts[5], out var projectId) &&
                DateTime.TryParse(parts[4], out var dueDate))
            {
                var title = parts[2];
                var description = parts[3] == "null" ? null : parts[3];

                var updateDto = new JobUpdateDto(title, description, dueDate, projectId);
                await _messageService.SendUpdateJobAsync(jobId, updateDto);
                Console.WriteLine($"Update job request sent: {jobId}");
            }
            else
            {
                Console.WriteLine("Invalid format - Usage: update <job_id> <title> <description> <due_date> <project_id>");
            }
        }
        else
        {
            Console.WriteLine("Usage: update <job_id> <title> <description> <due_date> <project_id>");
        }
    }

    private async void HandleDeleteCommand(string[] parts)
    {
        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var deleteId))
        {
            await _messageService.SendDeleteJobAsync(deleteId);
            Console.WriteLine($"Delete job request sent: {deleteId}");
        }
        else
        {
            Console.WriteLine("Usage: delete <job_id>");
        }
    }

    private async void HandleGetCommand(string[] parts)
    {
        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var getId))
        {
            var response = await _messageService.GetJobAsync(getId);
            if (response != null)
            {
                Console.WriteLine($"Job details: {response}");
            }
            else
            {
                Console.WriteLine($"Failed to retrieve job: {getId}");
            }
        }
        else
        {
            Console.WriteLine("Usage: get <job_id>");
        }
    }

    private async void HandleListCommand(string[] parts)
    {
        var page = parts.Length > 1 ? int.Parse(parts[1]) : 1;
        var size = parts.Length > 2 ? int.Parse(parts[2]) : 10;

        var response = await _messageService.GetAllJobsAsync(page, size);
        if (response != null)
        {
            Console.WriteLine($"Jobs list (page {page}, size {size}): {response}");
        }
        else
        {
            Console.WriteLine($"Failed to retrieve jobs list (page {page}, size {size})");
        }
    }

    private async void HandleCreateProjectCommand(string[] parts)
    {
        if (parts.Length >= 2)
        {
            var name = string.Join(" ", parts.Skip(1)); // Join all remaining parts as the project name
            var createDto = new ProjectCreateDto(name);
            await _messageService.SendCreateProjectAsync(createDto);
            Console.WriteLine($"Create project request sent: {name}");
        }
        else
        {
            Console.WriteLine("Usage: createproject <project_name>");
        }
    }

    private async void HandleUpdateProjectCommand(string[] parts)
    {
        if (parts.Length >= 3)
        {
            if (Guid.TryParse(parts[1], out var projectId))
            {
                var name = parts[2];
                var description = parts.Length > 3 ? string.Join(" ", parts.Skip(3)) : null;

                var updateDto = new ProjectUpdateDto(name, description);
                await _messageService.SendUpdateProjectAsync(projectId, updateDto);
                Console.WriteLine($"Update project request sent: {projectId}");
            }
            else
            {
                Console.WriteLine("Invalid project ID format");
            }
        }
        else
        {
            Console.WriteLine("Usage: updateproject <project_id> <name> [description]");
        }
    }

    private async void HandleDeleteProjectCommand(string[] parts)
    {
        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var projectId))
        {
            await _messageService.SendDeleteProjectAsync(projectId);
            Console.WriteLine($"Delete project request sent: {projectId}");
        }
        else
        {
            Console.WriteLine("Usage: deleteproject <project_id>");
        }
    }

    private async void HandleGetProjectCommand(string[] parts)
    {
        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var projectId))
        {
            var response = await _messageService.GetProjectAsync(projectId);
            if (response != null)
            {
                Console.WriteLine($"Project details: {response}");
            }
            else
            {
                Console.WriteLine($"Failed to retrieve project: {projectId}");
            }
        }
        else
        {
            Console.WriteLine("Usage: getproject <project_id>");
        }
    }

    private async void HandleListProjectsCommand(string[] parts)
    {
        var page = parts.Length > 1 ? int.Parse(parts[1]) : 1;
        var size = parts.Length > 2 ? int.Parse(parts[2]) : 10;

        var response = await _messageService.GetAllProjectsAsync(page, size);
        if (response != null)
        {
            Console.WriteLine($"Projects list (page {page}, size {size}): {response}");
        }
        else
        {
            Console.WriteLine($"Failed to retrieve projects list (page {page}, size {size})");
        }
    }

    private void HandleExitCommand()
    {
        Console.WriteLine("Exiting...");
        IsExitCommand = true;
    }

    public void ShowHelp()
    {
        Console.WriteLine("Authentication Commands:");
        Console.WriteLine("1. register <username> <email> <password> - Register a new user");
        Console.WriteLine("2. login <email> <password> - Login with credentials");
        Console.WriteLine("3. logout - Logout current user");
        Console.WriteLine();
        Console.WriteLine("Job Commands (require authentication):");
        Console.WriteLine("4. create <title> <project_id> - Create a new job");
        Console.WriteLine("5. update <job_id> <title> <description> <due_date> <project_id> - Update a job");
        Console.WriteLine("6. delete <job_id> - Delete a job");
        Console.WriteLine("7. get <job_id> - Get a specific job");
        Console.WriteLine("8. list [page] [size] - List all jobs (default: page 1, size 10)");
        Console.WriteLine();
        Console.WriteLine("Project Commands (require authentication):");
        Console.WriteLine("9. createproject <project_name> - Create a new project");
        Console.WriteLine("10. updateproject <project_id> <name> [description] - Update a project");
        Console.WriteLine("11. deleteproject <project_id> - Delete a project");
        Console.WriteLine("12. getproject <project_id> - Get a specific project");
        Console.WriteLine("13. listprojects [page] [size] - List all projects (default: page 1, size 10)");
        Console.WriteLine("14. exit - Exit the application");
        Console.WriteLine();
    }
}