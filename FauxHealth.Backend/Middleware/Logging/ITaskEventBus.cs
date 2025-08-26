using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace FauxHealth.Backend.Middleware.Logging;

public interface ITaskEventBus
{
    void Publish(TaskEvent evt);
    IAsyncEnumerable<TaskEvent> Subscribe(CorrelationId taskId, CancellationToken ct);
}

public sealed class InMemoryTaskEventBus : ITaskEventBus
{
    private readonly ConcurrentDictionary<Guid, Channel<TaskEvent>> _channels = new();

    public void Publish(TaskEvent evt)
    {
        if (_channels.TryGetValue(evt.TaskId, out var channel))
            channel.Writer.TryWrite(evt);
    }

    public async IAsyncEnumerable<TaskEvent> Subscribe(CorrelationId taskId, [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<TaskEvent>();
        _channels[taskId] = channel;

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
                yield return evt;
        }
        finally
        {
            _channels.TryRemove(taskId, out _);
        }
    }
} 