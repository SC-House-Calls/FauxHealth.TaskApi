using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace FauxHealth.Backend.Middleware.Auditing.FieldChangesAuditing;

public interface IDomainUpdateAuditor<in TUpdate> : IDomainUpdateAuditor where TUpdate : IDomainUpdate
{
    Task<ImmutableArray<FieldChangeAudit>> AuditAsync(TUpdate update, CorrelationId? taskId, CorrelationId? stepId, CancellationToken ct);
}

public interface IDomainUpdateAuditor
{
    Task<ImmutableArray<FieldChangeAudit>> AuditAsync(IDomainUpdate update, CorrelationId? taskId, CorrelationId? stepId, CancellationToken ct);
}

public sealed class DomainUpdateAuditDispatcher(IServiceProvider sp)
{
    public Task<ImmutableArray<FieldChangeAudit>[]> DispatchAsync(IEnumerable<IDomainUpdate> updates, CorrelationId? taskId, CorrelationId? stepId, CancellationToken ct)
    {
        var tasks = new List<Task<ImmutableArray<FieldChangeAudit>>>();
        foreach (var update in updates)
        {
            var uType = update.GetType();
            var handlerType = typeof(IDomainUpdateAuditor<>).MakeGenericType(uType);
            foreach (var handler in sp.GetServices(handlerType))
            {
                if (handler is not IDomainUpdateAuditor auditor) throw new InvalidOperationException();
                tasks.Add(auditor.AuditAsync(update, taskId, stepId, ct));
            }
        }
        return Task.WhenAll(tasks);
    }
}