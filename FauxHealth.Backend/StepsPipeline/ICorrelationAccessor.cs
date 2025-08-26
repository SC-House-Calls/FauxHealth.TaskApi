namespace FauxHealth.Backend.StepsPipeline;

public interface ICorrelationAccessor
{
    CorrelationId? TaskId { get; }
    CorrelationId? StepId { get; }
    IDisposable UseTask(CorrelationId taskId);
    IDisposable UseStep(CorrelationId stepId);
}

public sealed class CorrelationAccessor : ICorrelationAccessor
{
    private static readonly AsyncLocal<CorrelationId?> _task = new();
    private static readonly AsyncLocal<CorrelationId?> _step = new();
    public CorrelationId? TaskId => _task.Value;
    public CorrelationId? StepId => _step.Value;
    public IDisposable UseTask(CorrelationId taskId) => Swap(_task, taskId);
    public IDisposable UseStep(CorrelationId stepId) => Swap(_step, stepId);

    private static IDisposable Swap(AsyncLocal<CorrelationId?> slot, CorrelationId value)
    {
        var prev = slot.Value;
        slot.Value = value;
        return new Scope(() => slot.Value = prev);
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}