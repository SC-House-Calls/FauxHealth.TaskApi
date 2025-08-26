using FauxHealth.Backend.Middleware;

namespace FauxHealth.Backend.TasksPipeline;

public sealed class TaskPipeline
{
    private readonly TaskDelegate _terminal;
    private readonly IReadOnlyList<ITaskMiddleware> _components;
    public TaskPipeline(IEnumerable<ITaskMiddleware> components, TaskDelegate terminal)
    {
        _components = components.Reverse().ToArray();
        _terminal = terminal;
    }
    public Task<TaskResponse> Invoke(ITaskContext ctx)
    {
        var next = _terminal;
        foreach (var m in _components) { var local = next; next = c => m.InvokeAsync(c, local); }
        return next(ctx);
    }
}