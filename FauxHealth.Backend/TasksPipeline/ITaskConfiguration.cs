namespace FauxHealth.Backend.TasksPipeline;

public interface ITaskConfiguration
{
    Type TaskRequestType { get; }
    void Configure(dynamic builder);
}

public abstract class TaskConfiguration<TRequest> : ITaskConfiguration where TRequest : TaskRequest
{
    public abstract void Configure(TaskTypeBuilder<TRequest> builder);
    public Type TaskRequestType => typeof(TRequest);
    void ITaskConfiguration.Configure(dynamic builder) => Configure((TaskTypeBuilder<TRequest>)builder);
}