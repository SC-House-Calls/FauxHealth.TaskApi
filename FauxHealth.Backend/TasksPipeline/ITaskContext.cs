using Microsoft.Extensions.DependencyInjection;

namespace FauxHealth.Backend.TasksPipeline;

public interface ITaskContext : IAsyncDisposable
{
    CorrelationId CorrelationId { get; }
    TaskRequest Request { get; }
    IServiceProvider ServiceProvider { get; }
    CancellationToken CancellationToken { get; }
}

public sealed class TaskContext<TRequest> : ITaskContext where TRequest : TaskRequest
{
    private readonly IServiceScope _scope;

    public TaskContext(TRequest request, IServiceScope scope, CancellationToken cancellationToken = default)
    {
        Request = request;
        ServiceProvider = scope.ServiceProvider;
        CancellationToken = cancellationToken;
        _scope = scope;
        CorrelationId = CorrelationId.NewId();
    }

    public CorrelationId CorrelationId { get; internal set; }
    public TRequest Request { get; }
    public IServiceProvider ServiceProvider { get; }
    public CancellationToken CancellationToken { get; }
    TaskRequest ITaskContext.Request => Request;

    public async ValueTask DisposeAsync()
    {
        if (_scope is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
        else _scope.Dispose();
    }
}