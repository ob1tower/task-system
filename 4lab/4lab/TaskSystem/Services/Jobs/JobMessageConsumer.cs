using Microsoft.Extensions.DependencyInjection;
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

namespace TaskSystem.Services.Jobs;

public interface IJobMessageConsumer
{
    void StartConsuming();
    void StopConsuming();
}

public class JobMessageConsumer : IJobMessageConsumer, IDisposable
{
    private readonly IModel _channel;
    private readonly IModel _dlqChannel; // Dead Letter Queue channel
    private readonly IConnection _connection;
    private readonly RabbitMqConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private EventingBasicConsumer? _consumer;
    private bool _isConsuming;
    private const string DeadLetterExchange = "dead_letter_exchange";
    private const string DeadLetterQueue = "dead_letter_queue";
    private const string DeadLetterRoutingKey = "dead.letter";
    private const string RequestQueueName = "api.requests";
    private const string ResponseQueueName = "api.responses";

    public JobMessageConsumer(RabbitMqConfiguration config, IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;

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
        _dlqChannel = _connection.CreateModel(); // Separate channel for DLQ operations

        // Declare Dead Letter Exchange and Queue
        _dlqChannel.ExchangeDeclare(exchange: DeadLetterExchange,
                                   type: ExchangeType.Direct,
                                   durable: true,
                                   autoDelete: false);

        _dlqChannel.QueueDeclare(queue: DeadLetterQueue,
                                durable: true,
                                exclusive: false,
                                autoDelete: false);

        _dlqChannel.QueueBind(queue: DeadLetterQueue,
                             exchange: DeadLetterExchange,
                             routingKey: DeadLetterRoutingKey);

        // Declare main exchange
        _channel.ExchangeDeclare(exchange: RabbitMqConfiguration.ExchangeName,
                                type: ExchangeType.Direct,
                                durable: true,
                                autoDelete: false);

        // Declare request queue with dead letter exchange configuration
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", DeadLetterExchange },
            { "x-dead-letter-routing-key", DeadLetterRoutingKey },
            { "x-message-ttl", 300000 } // 5 minutes TTL for unprocessed messages
        };

        _channel.QueueDeclare(queue: RequestQueueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: args);

        _channel.QueueBind(queue: RequestQueueName,
                          exchange: RabbitMqConfiguration.ExchangeName,
                          routingKey: "api.requests");
    }

    public void StartConsuming()
    {
        if (_isConsuming) return;

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += async (model, ea) =>
        {
            // Get the message processing service from the service provider
            using var scope = _serviceProvider.CreateScope();
            var messageProcessingService = scope.ServiceProvider.GetRequiredService<IMessageProcessingService>();

            // Delegate the message processing to the dedicated service
            await messageProcessingService.ProcessMessageAsync(ea, _serviceProvider, _channel);
        };

        _channel.BasicConsume(queue: RequestQueueName,
                             autoAck: false,
                             consumer: _consumer);

        _isConsuming = true;
        Log.Logger.Information("Job message consumer started. Listening on queue: {QueueName}", RequestQueueName);
    }

    public void StopConsuming()
    {
        if (!_isConsuming || _consumer == null) return;

        _channel.BasicCancel(_consumer.ConsumerTags.First());
        _isConsuming = false;
        Log.Logger.Information("Job message consumer stopped");
    }

    public void Dispose()
    {
        StopConsuming();
        _channel?.Close();
        _dlqChannel?.Close();
        _connection?.Close();
    }
}