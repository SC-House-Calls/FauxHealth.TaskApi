using Microsoft.Extensions.DependencyInjection;

namespace FauxHealth.Backend.TasksPipeline;

public sealed class TaskContextFactory(IServiceProvider root)
{
    public TaskContext<TRequest> Create<TRequest>(TRequest request, CancellationToken ct = default)
        where TRequest : TaskRequest
        => new(request, root.CreateScope(), ct);
}