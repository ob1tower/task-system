using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TaskSystemClient.Models;
using TaskSystemClient.Models.Job;
using TaskSystemClient.Models.Project;
using TaskSystemClient.Models.User;
using TaskSystemClient.Services;

namespace TaskSystemClient.Services;

public class UnifiedMessageService : IMessageService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqConfig _config;
    private readonly string _replyQueueName;
    private readonly Dictionary<string, TaskCompletionSource<string?>> _pendingRequests;
    private EventingBasicConsumer? _consumer;
    private readonly IAuthService? _authService;
    private readonly object _lock = new object();
    private bool _disposed = false;

    public UnifiedMessageService(RabbitMqConfig config, IAuthService? authService = null)
    {
        _config = config;
        _authService = authService;
        _pendingRequests = new Dictionary<string, TaskCompletionSource<string?>>();

        var factory = new ConnectionFactory
        {
            HostName = _config.HostName,
            Port = _config.Port,
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange
        _channel.ExchangeDeclare(exchange: _config.ExchangeName,
                                type: ExchangeType.Direct,
                                durable: true,
                                autoDelete: false);

        // Declare a server-named queue for responses
        _replyQueueName = _channel.QueueDeclare("", false, true, true).QueueName;

        // Start consuming responses
        StartResponseConsumer();
    }

    private void StartResponseConsumer()
    {
        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var responseJson = Encoding.UTF8.GetString(body);

            // Extract correlation ID from message properties
            var correlationId = ea.BasicProperties.CorrelationId;

            TaskCompletionSource<string?>? tcs = null;

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(correlationId) && _pendingRequests.TryGetValue(correlationId, out tcs))
                {
                    // Remove the request from pending requests
                    _pendingRequests.Remove(correlationId);
                }
            }

            if (tcs != null)
            {
                tcs.SetResult(responseJson);
            }
            else
            {
                // Even if no specific request is waiting, we display the response
                Console.WriteLine($"Received unsolicited response: {responseJson}");
            }

            // Acknowledge the message
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(_replyQueueName, false, _consumer);
    }

    private async Task<string?> SendStandardRequestAsync(string action, object? data, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string?>();

        lock (_lock)
        {
            _pendingRequests[correlationId] = tcs;
        }

        // Create standard request message
        var request = new StandardRequestMessage
        {
            Action = action,
            Data = data,
            Auth = _authService?.GetAccessToken() != null ? $"Bearer {_authService.GetAccessToken()}" : null
        };

        var json = JsonSerializer.Serialize(request);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.ReplyTo = _replyQueueName;
        properties.CorrelationId = correlationId;
        properties.Persistent = true;

        _channel.BasicPublish(exchange: _config.ExchangeName,
                             routingKey: "api.requests", // Using the new queue name
                             basicProperties: properties,
                             body: body);

        // Wait for response or cancellation
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

        try
        {
            return await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Remove the pending request if timed out
            lock (_lock)
            {
                _pendingRequests.Remove(correlationId);
            }
            Console.WriteLine($"Request {action} timed out.");
            return null;
        }
    }

    // Authentication operations
    public async Task<RegisterResponseMessage?> RegisterAsync(UserRegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var data = new {
            user_name = registerDto.UserName,
            email = registerDto.Email,
            password = registerDto.Password
        };

        var responseJson = await SendStandardRequestAsync("register_user", data, cancellationToken);

        if (responseJson != null)
        {
            var standardResponse = JsonSerializer.Deserialize<StandardResponseMessage>(responseJson);
            if (standardResponse?.Status == "ok")
            {
                return new RegisterResponseMessage
                {
                    Success = true,
                    Message = standardResponse.Data?.ToString() ?? "Registration successful",
                    MessageId = Guid.NewGuid()
                };
            }
            else if (standardResponse?.Status == "error")
            {
                return new RegisterResponseMessage
                {
                    Success = false,
                    Message = standardResponse.Error ?? "Registration failed",
                    MessageId = Guid.NewGuid()
                };
            }
        }

        return null;
    }

    public async Task<LoginResponseMessage?> LoginAsync(UserLoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var data = new {
            email = loginDto.Email,
            password = loginDto.Password
        };

        var responseJson = await SendStandardRequestAsync("login_user", data, cancellationToken);

        if (responseJson != null)
        {
            var standardResponse = JsonSerializer.Deserialize<StandardResponseMessage>(responseJson);
            if (standardResponse?.Status == "ok" && standardResponse.Data != null)
            {
                // Extract access token from the response data
                var dataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(standardResponse.Data));
                string? accessToken = null;

                if (dataDict != null && dataDict.ContainsKey("access_token"))
                {
                    accessToken = dataDict["access_token"]?.ToString();
                }

                return new LoginResponseMessage
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    MessageId = Guid.NewGuid()
                };
            }
            else if (standardResponse?.Status == "error")
            {
                return new LoginResponseMessage
                {
                    Success = false,
                    Message = standardResponse.Error ?? "Login failed",
                    MessageId = Guid.NewGuid()
                };
            }
        }

        return null;
    }

    // Операции с задачами
    public async Task SendCreateJobAsync(JobCreateDto jobCreateDto, CancellationToken cancellationToken = default)
    {
        var data = new {
            title = jobCreateDto.Title,
            project_id = jobCreateDto.ProjectId
        };
        await SendStandardRequestAsync("create_job", data, cancellationToken);
    }

    public async Task SendUpdateJobAsync(Guid jobId, JobUpdateDto jobUpdateDto, CancellationToken cancellationToken = default)
    {
        var data = new {
            job_id = jobId,
            title = jobUpdateDto.Title,
            description = jobUpdateDto.Description,
            due_date = jobUpdateDto.DueDate,
            project_id = jobUpdateDto.ProjectId
        };
        await SendStandardRequestAsync("update_job", data, cancellationToken);
    }

    public async Task SendDeleteJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var data = new { job_id = jobId };
        await SendStandardRequestAsync("delete_job", data, cancellationToken);
    }

    public async Task<string?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var data = new { job_id = jobId };
        return await SendStandardRequestAsync("get_job", data, cancellationToken);
    }

    public async Task<string?> GetAllJobsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var data = new { page_number = pageNumber, page_size = pageSize };
        return await SendStandardRequestAsync("get_all_jobs", data, cancellationToken);
    }

    // Project operations
    public async Task SendCreateProjectAsync(ProjectCreateDto projectCreateDto, CancellationToken cancellationToken = default)
    {
        var data = new {
            name = projectCreateDto.Name
        };
        await SendStandardRequestAsync("create_project", data, cancellationToken);
    }

    public async Task SendUpdateProjectAsync(Guid projectId, ProjectUpdateDto projectUpdateDto, CancellationToken cancellationToken = default)
    {
        var data = new {
            project_id = projectId,
            name = projectUpdateDto.Name,
            description = projectUpdateDto.Description
        };
        await SendStandardRequestAsync("update_project", data, cancellationToken);
    }

    public async Task SendDeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var data = new { project_id = projectId };
        await SendStandardRequestAsync("delete_project", data, cancellationToken);
    }

    public async Task<string?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var data = new { project_id = projectId };
        return await SendStandardRequestAsync("get_project", data, cancellationToken);
    }

    public async Task<string?> GetAllProjectsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var data = new { page_number = pageNumber, page_size = pageSize };
        return await SendStandardRequestAsync("get_all_projects", data, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // EventingBasicConsumer does not implement IDisposable in this version of RabbitMQ.Client
        _channel?.Close();
        _connection?.Close();
    }
}