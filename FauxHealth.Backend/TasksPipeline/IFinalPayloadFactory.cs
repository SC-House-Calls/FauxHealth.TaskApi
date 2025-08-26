namespace FauxHealth.Backend.TasksPipeline;

public interface IFinalPayloadFactory<TRequest, TPayload>
    where TRequest : TaskRequest where TPayload : notnull
{
    Task<TPayload> CreateAsync(TaskContext<TRequest> context, CancellationToken ct);
}