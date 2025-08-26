namespace FauxHealth.Backend.StepsPipeline;

public sealed record StepNode(Type StepProcessorType, ISet<Type> Dependencies)
{
    public Type? CompensationStepType { get; init; }
}