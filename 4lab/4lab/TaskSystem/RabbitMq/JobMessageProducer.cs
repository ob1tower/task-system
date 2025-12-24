using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TaskSystem.RabbitMq.Messages;

namespace TaskSystem.RabbitMq;

public interface IJobMessageProducer
{
    void SendCreateJobMessage(CreateJobMessage message);
    void SendUpdateJobMessage(UpdateJobMessage message);
    void SendDeleteJobMessage(DeleteJobMessage message);
    void SendGetJobMessage(GetJobMessage message);
    void SendGetAllJobsMessage(GetAllJobsMessage message);
}

public class JobMessageProducer : IJobMessageProducer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqConfiguration _config;

    public JobMessageProducer(RabbitMqConfiguration config)
    {
        _config = config;

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

        _channel.ExchangeDeclare(exchange: RabbitMqConfiguration.ExchangeName,
                                type: ExchangeType.Direct,
                                durable: true,
                                autoDelete: false);

        // Declare queue with same configuration as in JobMessageConsumer to prevent conflicts
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "dead_letter_exchange" },
            { "x-dead-letter-routing-key", "dead.letter" },
            { "x-message-ttl", 300000 } // 5 minutes TTL for unprocessed messages
        };

        _channel.QueueDeclare(queue: RabbitMqConfiguration.JobQueueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: args);

        _channel.QueueBind(queue: RabbitMqConfiguration.JobQueueName,
                          exchange: RabbitMqConfiguration.ExchangeName,
                          routingKey: RabbitMqConfiguration.RoutingKeyName);
    }

    public void SendCreateJobMessage(CreateJobMessage message)
    {
        message.Operation = "create_job";
        SendMessage(message);
    }

    public void SendUpdateJobMessage(UpdateJobMessage message)
    {
        message.Operation = "update_job";
        SendMessage(message);
    }

    public void SendDeleteJobMessage(DeleteJobMessage message)
    {
        message.Operation = "delete_job";
        SendMessage(message);
    }

    public void SendGetJobMessage(GetJobMessage message)
    {
        message.Operation = "get_job";
        SendMessage(message);
    }

    public void SendGetAllJobsMessage(GetAllJobsMessage message)
    {
        message.Operation = "get_all_jobs";
        SendMessage(message);
    }

    private void SendMessage<T>(T message) where T : JobMessageBase
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(exchange: RabbitMqConfiguration.ExchangeName,
                             routingKey: RabbitMqConfiguration.RoutingKeyName,
                             basicProperties: properties,
                             body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}