using FauxHealth.Backend.Middleware.Logging;
using FauxHealth.Backend.StepsPipeline;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.Extensions.Logging;
using static FauxHealth.Backend.Middleware.Auditing.TaskAuditSinkRecord;

namespace FauxHealth.Backend.Middleware.Auditing;

public sealed class TaskAuditingMiddleware(
    IAuditSink sink, 
    ICorrelationAccessor corr,
    ITaskEventBus eventBus,
    ILogger<TaskAuditingMiddleware> log)
    : ITaskMiddleware
{
    public async Task<TaskResponse> InvokeAsync(ITaskContext context, TaskDelegate next)
    {
        using var _ = corr.UseTask(context.CorrelationId);
        var started = DateTimeOffset.UtcNow;

        var startedRecord = new TaskAuditStartedSinkRecord(
            taskId: context.CorrelationId,
            request: context.Request.GetType().Name,
            createdBy: context.Request.CreatedBy,
            createdAt: context.Request.CreatedAt,
            startedAt: started);
        
        await sink.WriteAsync(startedRecord, context.CancellationToken);

        eventBus.Publish(new TaskEvent(TaskEventType.TaskAudit, context.CorrelationId, startedRecord.CreatedAt, startedRecord));
        eventBus.Publish(new TaskEvent(TaskEventType.StatusChanged, context.CorrelationId, startedRecord.StartedAt, new { Status = "Started" }));
        
        try
        {
            var result = await next(context);
            
            var completedRecord = new TaskAuditCompletedSinkRecord(
                taskId: context.CorrelationId,
                result: result.GetType().Name,
                completedAt: DateTimeOffset.UtcNow,
                durationMs: (DateTimeOffset.UtcNow - started).TotalMilliseconds);
            
            await sink.WriteAsync(completedRecord, context.CancellationToken);
            eventBus.Publish(new TaskEvent(TaskEventType.TaskAudit, context.CorrelationId, completedRecord.CompletedAt, completedRecord));
            eventBus.Publish(new TaskEvent(TaskEventType.StatusChanged, context.CorrelationId, completedRecord.CompletedAt, new { Status = "Completed" }));
            
            return result;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Task {TaskId} failed", context.CorrelationId);
            
            var failedRecord = new TaskAuditFailedSinkRecord(
                taskId: context.CorrelationId,
                error: ex.Message,
                failedAt: DateTimeOffset.UtcNow);
            
            await sink.WriteAsync(failedRecord, context.CancellationToken);
            eventBus.Publish(new TaskEvent(TaskEventType.TaskAudit, context.CorrelationId, failedRecord.FailedAt, failedRecord));
            eventBus.Publish(new TaskEvent(TaskEventType.StatusChanged, context.CorrelationId, failedRecord.FailedAt, new { Status = "Failed" }));
            throw;
        }
    }
}