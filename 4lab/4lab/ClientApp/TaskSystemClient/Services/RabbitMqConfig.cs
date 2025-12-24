namespace TaskSystemClient.Services;

public class RabbitMqConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "task_system_exchange";
    public string JobQueueName { get; set; } = "job_operations_queue";
    public string RoutingKeyName { get; set; } = "job.operations";
}