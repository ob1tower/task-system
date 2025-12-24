using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;
using System.Text.Json;
using TaskSystem.RabbitMq;
using TaskSystem.RabbitMq.Messages;
using TaskSystem.Services.Jobs.Interfaces;
using TaskSystem.Services.Interfaces;
using TaskSystem.Services.Projects.Interfaces;
using TaskSystem.Dtos.Job;
using TaskSystem.Dtos.Project;
using TaskSystem.Dtos.User;

namespace TaskSystem.Services.Jobs;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync(BasicDeliverEventArgs ea, IServiceProvider serviceProvider, IModel channel);
}

public class MessageProcessingService : IMessageProcessingService
{
    private const int MaxRetryCount = 3;
    private const int BaseDelayMs = 1000; // 1 second base delay for exponential backoff

    public async Task ProcessMessageAsync(BasicDeliverEventArgs ea, IServiceProvider serviceProvider, IModel channel)
    {
        var body = ea.Body.ToArray();
        var messageJson = Encoding.UTF8.GetString(body);

        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IMessageBasedJobService>();
            var userService = scope.ServiceProvider.GetRequiredService<IUsersService>();
            var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
            var idempotencyService = scope.ServiceProvider.GetService<IIdempotencyService>(); // Optional service

            // Deserialize standard request message
            var standardRequest = JsonSerializer.Deserialize<StandardRequestMessage>(messageJson);
            if (standardRequest == null)
            {
                Log.Logger.Error("Failed to deserialize standard request message: {Message}", messageJson);
                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            // Check for idempotency - if message was already processed, return success response
            if (idempotencyService != null)
            {
                if (await idempotencyService.IsProcessedAsync(standardRequest.Id))
                {
                    Log.Logger.Information("Idempotency check: Message {MessageId} already processed, returning cached response", standardRequest.Id);

                    var idempotentResponse = new StandardResponseMessage
                    {
                        CorrelationId = standardRequest.Id,
                        Status = "ok",
                        Data = new { success = true, message = "Request already processed (idempotency)", is_cached = true },
                        Error = null
                    };

                    await SendStandardResponseAsync(idempotentResponse, ea, channel);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }
            }

            var action = standardRequest.Action;

            // Verify authentication if provided
            string? token = null;
            if (!string.IsNullOrEmpty(standardRequest.Auth))
            {
                token = standardRequest.Auth;
            }

            // Process the action based on type
            var (isSuccess, resultData, errorMessage) = await ProcessActionAsync(
                action, standardRequest, jobService, userService, projectService, idempotencyService, token);

            // Mark message as processed if using idempotency service and operation was successful
            if (idempotencyService != null && isSuccess)
            {
                await idempotencyService.MarkAsProcessedAsync(standardRequest.Id);
            }

            var response = new StandardResponseMessage
            {
                CorrelationId = standardRequest.Id,
                Status = isSuccess ? "ok" : "error",
                Data = resultData,
                Error = isSuccess ? null : errorMessage
            };

            await SendStandardResponseAsync(response, ea, channel);

            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            Log.Logger.Information("Message processed successfully. Action: {Action}", action);
        }
        catch (JsonException ex)
        {
            Log.Logger.Error(ex, "Failed to deserialize message JSON. Message content: {Message}", messageJson);

            var errorResponse = new StandardResponseMessage
            {
                CorrelationId = Guid.Empty, // Can't get original ID if deserialization failed
                Status = "error",
                Error = "Invalid JSON format"
            };

            // Send error response if we can extract correlation ID from raw JSON
            try
            {
                using var document = JsonDocument.Parse(messageJson);
                var root = document.RootElement;
                if (root.TryGetProperty("id", out var idElement))
                {
                    errorResponse.CorrelationId = Guid.Parse(idElement.GetString()!);
                }
            }
            catch { }

            await SendStandardResponseAsync(errorResponse, ea, channel);

            // Send to dead letter queue since this is a permanent error
            SendToDeadLetterQueue(channel, messageJson, ex.Message);
            channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error processing message. Delivery tag: {DeliveryTag}", ea.DeliveryTag);

            var errorResponse = new StandardResponseMessage
            {
                CorrelationId = Guid.Empty, // Will try to extract from original message
                Status = "error",
                Error = ex.Message
            };

            // Try to extract correlation ID from original message
            try
            {
                using var document = JsonDocument.Parse(messageJson);
                var root = document.RootElement;
                if (root.TryGetProperty("id", out var idElement))
                {
                    errorResponse.CorrelationId = Guid.Parse(idElement.GetString()!);
                }
            }
            catch { }

            await SendStandardResponseAsync(errorResponse, ea, channel);

            // Check the number of previous deliveries to decide whether to retry or send to DLQ
            var retryCount = GetRetryCount(ea);

            if (retryCount >= MaxRetryCount)
            {
                Log.Logger.Warning("Message failed after {MaxRetryCount} retries. Sending to DLQ. Delivery tag: {DeliveryTag}",
                    MaxRetryCount, ea.DeliveryTag);
                SendToDeadLetterQueue(channel, messageJson, ex.Message);
                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
            else
            {
                Log.Logger.Information("Requeuing message after failure. Retry {RetryCount}/{MaxRetryCount}. Delivery tag: {DeliveryTag}",
                    retryCount + 1, MaxRetryCount, ea.DeliveryTag);

                // Calculate delay for exponential backoff
                var delay = CalculateDelay(retryCount);
                await Task.Delay(delay);

                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    private async Task<(bool isSuccess, object? resultData, string? errorMessage)> ProcessActionAsync(
        string action, StandardRequestMessage request,
        IMessageBasedJobService jobService, IUsersService userService, IProjectService projectService,
        IIdempotencyService? idempotencyService, string? token)
    {
        try
        {
            switch (action)
            {
                case "create_job":
                    if (string.IsNullOrEmpty(token))
                    {
                        return (false, null, "Authentication required for job creation");
                    }

                    // Extract data for job creation from request.Data as dictionary
                    var createJobDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                    if (createJobDict != null && createJobDict.ContainsKey("title") && createJobDict.ContainsKey("project_id"))
                    {
                        var projectIdStr = createJobDict["project_id"].ToString();
                        if (Guid.TryParse(projectIdStr, out Guid createJobProjectId))
                        {
                            var createJobData = new JobCreateDto(
                                createJobDict["title"].ToString() ?? string.Empty,
                                createJobProjectId
                            );
                            var result = await jobService.CreateJobAsync(createJobData);
                            return (result.IsSuccess, result.IsSuccess ? new { job_id = result.Value, message = "Job created successfully" } : null, result.IsSuccess ? null : result.Error);
                        }
                    }
                    return (false, null, "Invalid create job data");

                case "update_job":
                    if (string.IsNullOrEmpty(token))
                    {
                        return (false, null, "Authentication required for job update");
                    }

                    var jobId = GetIdFromData(request.Data, "job_id");
                    if (jobId != Guid.Empty)
                    {
                        // Extract data for job update from request.Data as dictionary
                        var updateJobDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                        if (updateJobDict != null && updateJobDict.ContainsKey("title") && updateJobDict.ContainsKey("description")
                            && updateJobDict.ContainsKey("due_date") && updateJobDict.ContainsKey("project_id"))
                        {
                            var projectIdStr = updateJobDict["project_id"].ToString();
                            var dueDateStr = updateJobDict["due_date"].ToString();

                            if (Guid.TryParse(projectIdStr, out Guid updateJobProjectId) && DateTime.TryParse(dueDateStr, out DateTime dueDate))
                            {
                                var updateJobData = new JobUpdateDto(
                                    updateJobDict["title"].ToString() ?? string.Empty,
                                    updateJobDict["description"].ToString() == "null" ? null : updateJobDict["description"].ToString(),
                                    dueDate,
                                    updateJobProjectId
                                );
                                var result = await jobService.UpdateJobAsync(jobId, updateJobData);
                                return (result.IsSuccess, result.IsSuccess ? new { success = true, message = "Job updated successfully" } : null, result.IsSuccess ? null : result.Error);
                            }
                        }
                    }
                    return (false, null, "Invalid job ID or update job data");

                case "delete_job":
                    if (string.IsNullOrEmpty(token))
                    {
                        return (false, null, "Authentication required for job deletion");
                    }

                    var deleteJobId = GetIdFromData(request.Data, "job_id");
                    if (deleteJobId != Guid.Empty)
                    {
                        var result = await jobService.DeleteJobAsync(deleteJobId);
                        return (result.IsSuccess, result.IsSuccess ? new { success = true, message = "Job deleted successfully" } : null, result.IsSuccess ? null : result.Error);
                    }
                    return (false, null, "Invalid job ID");

                case "get_job":
                    var getJobId = GetIdFromData(request.Data, "job_id");
                    if (getJobId != Guid.Empty)
                    {
                        var result = await jobService.GetJobAsync(getJobId);
                        return (result.IsSuccess, result.IsSuccess ? result.Value : null, result.IsSuccess ? null : result.Error);
                    }
                    return (false, null, "Invalid job ID");

                case "get_all_jobs":
                    var jobPaginationData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                    int jobPage = 1, jobSize = 10;
                    if (jobPaginationData != null)
                    {
                        if (jobPaginationData.ContainsKey("page_number"))
                        {
                            var pageNumValue = jobPaginationData["page_number"];
                            if (pageNumValue is JsonElement pageNumElement)
                            {
                                jobPage = pageNumElement.GetInt32();
                            }
                            else
                            {
                                jobPage = Convert.ToInt32(pageNumValue);
                            }
                        }
                        if (jobPaginationData.ContainsKey("page_size"))
                        {
                            var pageSizeValue = jobPaginationData["page_size"];
                            if (pageSizeValue is JsonElement pageSizeElement)
                            {
                                jobSize = pageSizeElement.GetInt32();
                            }
                            else
                            {
                                jobSize = Convert.ToInt32(pageSizeValue);
                            }
                        }
                    }

                    var jobListResult = await jobService.GetAllJobsAsync(jobPage, jobSize);
                    return (jobListResult.IsSuccess, jobListResult.IsSuccess ? jobListResult.Value : null, jobListResult.IsSuccess ? null : jobListResult.Error);

                case "create_project":
                    // Check authentication for project creation
                    Console.WriteLine($"Create project - Token: {token?.Substring(0, Math.Min(20, token.Length))}...");
                    if (string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine("Create project - No token provided");
                        return (false, null, "Authentication required for project creation");
                    }

                    // Extract data for project creation from request.Data as dictionary
                    var createProjectDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                    if (createProjectDict != null && createProjectDict.ContainsKey("name"))
                    {
                        Console.WriteLine($"Create project - Name: {createProjectDict["name"]}");
                        var createProjectData = new ProjectCreateDto(
                            createProjectDict["name"].ToString() ?? string.Empty
                        );
                        var result = await projectService.CreateProjects(createProjectData);
                        Console.WriteLine($"Create project - Result: {result.IsSuccess}, Value: {result.Value}");
                        return (result.IsSuccess, result.IsSuccess ? new { projectId = result.Value } : null, result.IsSuccess ? null : result.Error);
                    }
                    Console.WriteLine("Create project - Invalid data");
                    return (false, null, "Invalid create project data");

                case "update_project":
                    if (string.IsNullOrEmpty(token))
                    {
                        return (false, null, "Authentication required for project update");
                    }

                    var projectId = GetIdFromData(request.Data, "project_id");
                    if (projectId != Guid.Empty)
                    {
                        // Extract data for project update from request.Data as dictionary
                        var updateProjectDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                        if (updateProjectDict != null && updateProjectDict.ContainsKey("name"))
                        {
                            var updateProjectData = new ProjectUpdateDto(
                                updateProjectDict["name"].ToString() ?? string.Empty,
                                updateProjectDict.ContainsKey("description") ?
                                    (updateProjectDict["description"].ToString() == "null" ? null : updateProjectDict["description"].ToString())
                                    : null
                            );
                            var result = await projectService.UpdateProjects(projectId, updateProjectData);
                            return (result.IsSuccess, result.IsSuccess ? new { success = true } : null, result.IsSuccess ? null : result.Error);
                        }
                    }
                    return (false, null, "Invalid project ID or update project data");

                case "delete_project":
                    if (string.IsNullOrEmpty(token))
                    {
                        return (false, null, "Authentication required for project deletion");
                    }

                    var deleteProjectId = GetIdFromData(request.Data, "project_id");
                    if (deleteProjectId != Guid.Empty)
                    {
                        var result = await projectService.DeleteProjects(deleteProjectId);
                        return (result.IsSuccess, result.IsSuccess ? new { success = true } : null, result.IsSuccess ? null : result.Error);
                    }
                    return (false, null, "Invalid project ID");

                case "get_project":
                    var getProjectId = GetIdFromData(request.Data, "project_id");
                    if (getProjectId != Guid.Empty)
                    {
                        var result = await projectService.GetProject(getProjectId);
                        return (result.IsSuccess, result.IsSuccess ? result.Value : null, result.IsSuccess ? null : result.Error);
                    }
                    return (false, null, "Invalid project ID");

                case "get_all_projects":
                    var projectPaginationData = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                    int projectPage = 1, projectSize = 10;
                    if (projectPaginationData != null)
                    {
                        if (projectPaginationData.ContainsKey("page_number"))
                        {
                            var pageNumValue = projectPaginationData["page_number"];
                            if (pageNumValue is JsonElement pageNumElement)
                            {
                                projectPage = pageNumElement.GetInt32();
                            }
                            else
                            {
                                projectPage = Convert.ToInt32(pageNumValue);
                            }
                        }
                        if (projectPaginationData.ContainsKey("page_size"))
                        {
                            var pageSizeValue = projectPaginationData["page_size"];
                            if (pageSizeValue is JsonElement pageSizeElement)
                            {
                                projectSize = pageSizeElement.GetInt32();
                            }
                            else
                            {
                                projectSize = Convert.ToInt32(pageSizeValue);
                            }
                        }
                    }

                    var projectListResult = await projectService.GetAllProjects(projectPage, projectSize);
                    return (projectListResult.IsSuccess,
                        projectListResult.IsSuccess ? new { projects = projectListResult.Value, total_count = projectListResult.Value?.Count } : null,
                        projectListResult.IsSuccess ? null : projectListResult.Error);

                case "register_user":
                    // Extract data for registration from request.Data as dictionary
                    var registerDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                    if (registerDict != null && registerDict.ContainsKey("user_name") && registerDict.ContainsKey("email") && registerDict.ContainsKey("password"))
                    {
                        var registerData = new UserRegisterDto(
                            registerDict["user_name"].ToString() ?? string.Empty,
                            registerDict["email"].ToString() ?? string.Empty,
                            registerDict["password"].ToString() ?? string.Empty
                        );
                        var result = await userService.Register(registerData);
                        return (result.IsSuccess, result.IsSuccess ? new { success = true } : null, result.IsSuccess ? null : result.Error);
                    }
                    return (false, null, "Invalid register data");

                case "login_user":
                    // Extract data for login from request.Data as dictionary
                    var loginDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request.Data));
                    if (loginDict != null && loginDict.ContainsKey("email") && loginDict.ContainsKey("password"))
                    {
                        var loginData = new UserLoginDto(
                            loginDict["email"].ToString() ?? string.Empty,
                            loginDict["password"].ToString() ?? string.Empty
                        );
                        var result = await userService.Login(loginData);
                        return (result.IsSuccess, result.IsSuccess ? new { access_token = result.Value?.AccessToken } : null, result.IsSuccess ? null : result.Error);
                    }
                    return (false, null, "Invalid login data");

                default:
                    Log.Logger.Warning("Unknown action received: {Action}", action);
                    return (false, null, $"Unknown action: {action}");
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error processing action {Action}", action);
            return (false, null, ex.Message);
        }
    }

    private async Task SendStandardResponseAsync(StandardResponseMessage response, BasicDeliverEventArgs ea, IModel channel)
    {
        var responseChannel = channel; // Using the same channel for response (in actual implementation, you might want a separate channel)

        // Send response to reply queue if specified, otherwise to default response queue
        string routingKey = !string.IsNullOrEmpty(ea.BasicProperties.ReplyTo)
            ? ea.BasicProperties.ReplyTo
            : "api.responses";

        var jsonResponse = JsonSerializer.Serialize(response);
        var body = Encoding.UTF8.GetBytes(jsonResponse);

        var replyProperties = responseChannel.CreateBasicProperties();
        replyProperties.CorrelationId = ea.BasicProperties.CorrelationId ?? response.CorrelationId.ToString();
        replyProperties.Persistent = true;

        responseChannel.BasicPublish(exchange: "", // Default exchange
                                    routingKey: routingKey,
                                    basicProperties: replyProperties,
                                    body: body);
    }

    private static Guid GetIdFromData(object? data, string idField)
    {
        if (data == null) return Guid.Empty;

        try
        {
            // First try to parse as dictionary
            if (data is Dictionary<string, object> dict)
            {
                if (dict.ContainsKey(idField))
                {
                    var value = dict[idField];
                    if (value is JsonElement element)
                    {
                        var idValue = element.GetString();
                        if (Guid.TryParse(idValue, out var guid))
                        {
                            return guid;
                        }
                    }
                    else
                    {
                        var idValue = value?.ToString();
                        if (Guid.TryParse(idValue, out var guid))
                        {
                            return guid;
                        }
                    }
                }
            }
            else
            {
                // Fallback to JSON parsing
                var json = JsonSerializer.Serialize(data);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty(idField, out var idElement))
                {
                    var idValue = idElement.GetString();
                    if (Guid.TryParse(idValue, out var guid))
                    {
                        return guid;
                    }
                }
            }
        }
        catch
        {
            // If parsing fails in any way, return empty Guid
        }

        return Guid.Empty;
    }

    private static int GetRetryCount(BasicDeliverEventArgs ea)
    {
        var headers = ea.BasicProperties.Headers;
        if (headers != null && headers.ContainsKey("x-death"))
        {
            var deathList = headers["x-death"] as object[];
            if (deathList != null && deathList.Length > 0)
            {
                // Get the latest death record from the array
                var deathDetails = deathList[0] as System.Collections.Generic.IDictionary<string, object>;
                if (deathDetails != null && deathDetails.ContainsKey("count"))
                {
                    return Convert.ToInt32(deathDetails["count"]);
                }
            }
        }
        return 0;
    }

    private static int CalculateDelay(int retryCount)
    {
        // Exponential backoff: 1s, 2s, 4s, etc.
        return BaseDelayMs * (int)Math.Pow(2, retryCount);
    }

    private static void SendToDeadLetterQueue(IModel channel, string messageJson, string errorReason)
    {
        const string DeadLetterExchange = "dead_letter_exchange";
        const string DeadLetterRoutingKey = "dead.letter";

        try
        {
            var body = Encoding.UTF8.GetBytes(messageJson);
            var properties = channel.CreateBasicProperties();
            properties.Headers = new Dictionary<string, object>
            {
                { "original_error", errorReason },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };

            channel.BasicPublish(exchange: DeadLetterExchange,
                               routingKey: DeadLetterRoutingKey,
                               basicProperties: properties,
                               body: body);

            Log.Logger.Warning("Message sent to Dead Letter Queue. Error: {Error}", errorReason);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to send message to Dead Letter Queue");
        }
    }
}