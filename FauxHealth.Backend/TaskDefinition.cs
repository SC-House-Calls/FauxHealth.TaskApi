using System.Collections.Frozen;
using FauxHealth.Backend.StepsPipeline;

namespace FauxHealth.Backend;

public sealed class TaskDefinition
{
    public FrozenDictionary<Type, StepNode> Steps { get; }
    public FrozenDictionary<Type, StepExecutionKind> ExecutionKinds { get; internal init; }
        = FrozenDictionary<Type, StepExecutionKind>.Empty;

    public TaskDefinition(IEnumerable<StepNode> steps)
        => Steps = steps.ToFrozenDictionary(s => s.StepProcessorType, s => s);
}