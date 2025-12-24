using System.Collections.Concurrent;

namespace TaskSystem.Services;

public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(Guid messageId);
    Task MarkAsProcessedAsync(Guid messageId);
}

public class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<Guid, bool> _processedMessages = new();

    public Task<bool> IsProcessedAsync(Guid messageId)
    {
        return Task.FromResult(_processedMessages.ContainsKey(messageId));
    }

    public Task MarkAsProcessedAsync(Guid messageId)
    {
        _processedMessages.TryAdd(messageId, true);
        return Task.CompletedTask;
    }
}