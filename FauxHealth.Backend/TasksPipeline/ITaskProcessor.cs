namespace FauxHealth.Backend.TasksPipeline;

public interface ITaskProcessor<TRequest> where TRequest : TaskRequest
{
    Task<TaskResponse> ProcessAsync(TaskContext<TRequest> context, CancellationToken cancellationToken);
}