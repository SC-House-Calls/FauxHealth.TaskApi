using System.Text.Json;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.EntityFrameworkCore;

namespace FauxHealth.Backend.Outbox;

public interface IOutboxWriter
{
    Task EnqueueAsync(CorrelationId taskId, CorrelationId stepId, Guid createdBy, Type stepProcessorType, TaskRequest request, CancellationToken ct);
}

public sealed class EfOutboxWriter(DbContext db, ITaskRequestSerializationRegistry registry) : IOutboxWriter
{
    public Task EnqueueAsync(CorrelationId taskId, CorrelationId stepId, Guid createdBy, Type stepProcessorType, TaskRequest request, CancellationToken ct)
    {
        var typeInfo = registry.GetTypeInfo(request.GetType());

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            JsonSerializer.Serialize(writer, request, typeInfo);
        }

        var msg = new OutboxMessage
        {
            TaskId = taskId,
            StepId = stepId,
            CreatedBy = createdBy,
            StepProcessorType = stepProcessorType.AssemblyQualifiedName!,
            RequestData = ms.ToArray()
        };
        
        db.Set<OutboxMessage>().Add(msg);
        return Task.CompletedTask; // SaveChanges handled by Saga transaction
    }
}