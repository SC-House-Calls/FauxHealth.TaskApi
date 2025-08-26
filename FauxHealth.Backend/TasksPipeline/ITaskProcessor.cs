namespace FauxHealth.Backend.TasksPipeline;

public interface ITaskProcessor<TRequest> : ITaskProcessor where TRequest : TaskRequest
{
    Task<TaskResponse> ProcessAsync(TaskContext<TRequest> context, CancellationToken cancellationToken);
}

public interface ITaskProcessor
{
    Task<TaskResponse> ProcessAsync(ITaskContext context, CancellationToken cancellationToken);
}