using Microsoft.Extensions.DependencyInjection;

namespace FauxHealth.Backend;

public interface IDomainUpdateAuditor<in TUpdate> where TUpdate : IDomainUpdate
{
    Task AuditAsync(TUpdate update, CorrelationId? taskId, CorrelationId? stepId, CancellationToken ct);
}

public sealed class DomainUpdateAuditDispatcher(IServiceProvider sp)
{
    public Task DispatchAsync(IEnumerable<IDomainUpdate> updates, CorrelationId? taskId, CorrelationId? stepId, CancellationToken ct)
    {
        var tasks = new List<Task>();
        foreach (var update in updates)
        {
            var uType = update.GetType();
            var handlerType = typeof(IDomainUpdateAuditor<>).MakeGenericType(uType);
            foreach (var handler in sp.GetServices(handlerType))
            {
                var method = handlerType.GetMethod("AuditAsync")!;
                tasks.Add((Task)method.Invoke(handler, [update, taskId!, stepId!, ct])!);
            }
        }
        return Task.WhenAll(tasks);
    }
}