using Microsoft.Extensions.Hosting;
using TaskSystem.Services.Jobs;

namespace TaskSystem.BackgroundServices;

public class JobConsumerHostedService : IHostedService
{
    private readonly IJobMessageConsumer _jobMessageConsumer;

    public JobConsumerHostedService(IJobMessageConsumer jobMessageConsumer)
    {
        _jobMessageConsumer = jobMessageConsumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _jobMessageConsumer.StartConsuming();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _jobMessageConsumer.StopConsuming();
        return Task.CompletedTask;
    }
}