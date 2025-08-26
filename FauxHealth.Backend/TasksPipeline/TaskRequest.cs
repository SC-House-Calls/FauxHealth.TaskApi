using ErrorOr;

namespace FauxHealth.Backend.TasksPipeline;

public abstract record TaskRequest(Guid CreatedBy)
{
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}

public abstract record TaskResponse(CorrelationId TaskId, Guid CreatedBy)
{
    public DateTimeOffset CompletedAt { get; } = DateTimeOffset.UtcNow;

    public sealed record Success<TPayload>(CorrelationId TaskId, Guid CreatedBy, TPayload Payload)
        : TaskResponse(TaskId, CreatedBy) where TPayload : notnull;

    public sealed record Failure(CorrelationId TaskId, Guid CreatedBy, Error Error)
        : TaskResponse(TaskId, CreatedBy);
}