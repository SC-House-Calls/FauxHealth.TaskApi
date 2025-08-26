using ErrorOr;
using FauxHealth.Backend.Middleware.Auditing.FieldChangesAuditing;
using FauxHealth.Backend.Middleware.Logging;
using FauxHealth.Backend.StepsPipeline;
using static FauxHealth.Backend.Middleware.Auditing.StepsAuditing.StepAuditSinkRecord;

namespace FauxHealth.Backend.Middleware.Auditing.StepsAuditing;

public sealed class StepAuditingMiddleware(
    IAuditSink sink,
    ICorrelationAccessor corr,
    IUpdateCollectorAccessor collectorAccessor,
    DomainUpdateAuditDispatcher dispatcher,
    ITaskEventBus eventBus) : IStepMiddleware
{
    public async Task<ErrorOr<Success>> InvokeAsync(StepExecutionContext ctx, StepDelegate next)
    {
        using var _ = corr.UseStep(ctx.StepCorrelationId);
        using var __ = collectorAccessor.Use(new UpdateCollector());

        var started = DateTimeOffset.UtcNow;
        var startedSinkRecord = new StepAuditStartedSinkRecord(ctx.TaskContext.CorrelationId, ctx.StepCorrelationId, 
            ctx.StepNode.StepProcessorType.Name, started);

        await sink.WriteAsync(startedSinkRecord, ctx.TaskContext.CancellationToken);
        eventBus.Publish(new TaskEvent(TaskEventType.StepAudit, ctx.TaskContext.CorrelationId, startedSinkRecord.StartedAt, startedSinkRecord));

        var result = await next(ctx);

        // Drain updates and dispatch to auditors, regardless of success/failure
        var updates = collectorAccessor.Current?.Drain() ?? [];
        if (updates.Count > 0)
        {
            var fieldChanges = await dispatcher.DispatchAsync(updates, corr.TaskId, corr.StepId, ctx.TaskContext.CancellationToken);
            foreach (var fieldChange in fieldChanges.SelectMany(x => x))
                eventBus.Publish(new TaskEvent(TaskEventType.FieldChangeAudit, ctx.TaskContext.CorrelationId,
                    fieldChange.ChangedAt, fieldChange));
        }
        
        var completedAt = DateTimeOffset.UtcNow;
        
        var completedSinkRecord = new StepAuditCompletedSinkRecord(ctx.TaskContext.CorrelationId, ctx.StepCorrelationId, 
            ctx.StepNode.StepProcessorType.Name, result.IsError ? "Error" : "Success", result.FirstError, 
            completedAt, (completedAt - started).TotalMilliseconds);

        await sink.WriteAsync(completedSinkRecord, ctx.TaskContext.CancellationToken);
        eventBus.Publish(new TaskEvent(TaskEventType.StepAudit, ctx.TaskContext.CorrelationId, completedSinkRecord.CompletedAt, completedSinkRecord));

        return result;
    }
}