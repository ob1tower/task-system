namespace TaskSystem.RabbitMq;

public class RabbitMqConfiguration
{
    public const string ExchangeName = "task_system_exchange";
    public const string JobQueueName = "job_operations_queue";
    public const string RoutingKeyName = "job.operations";
    
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}