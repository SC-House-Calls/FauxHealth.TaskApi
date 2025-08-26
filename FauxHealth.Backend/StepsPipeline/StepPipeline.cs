using ErrorOr;
using FauxHealth.Backend.Middleware;

namespace FauxHealth.Backend.StepsPipeline;

public sealed class StepPipeline
{
    private readonly StepDelegate _terminal;
    private readonly IReadOnlyList<IStepMiddleware> _components;
    public StepPipeline(IEnumerable<IStepMiddleware> components, StepDelegate terminal)
    {
        _components = components.Reverse().ToArray();
        _terminal = terminal;
    }
    public Task<ErrorOr<Success>> Invoke(StepExecutionContext ctx)
    {
        var next = _terminal;
        foreach (var m in _components)
        {
            var local = next; 
            next = c => m.InvokeAsync(c, local);
        }
        return next(ctx);
    }
}