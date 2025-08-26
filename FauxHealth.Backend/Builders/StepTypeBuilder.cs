using FauxHealth.Backend.StepsPipeline;

namespace FauxHealth.Backend.Builders;

public sealed class StepTypeBuilder
{
    internal Type ProcessorType { get; }
    internal ISet<Type> Dependencies { get; } = new HashSet<Type>();
    internal StepExecutionKind ExecutionKind { get; private set; } = StepExecutionKind.Internal;
    internal Type? CompensationType { get; private set; }

    internal StepTypeBuilder(Type processorType) => ProcessorType = processorType;

    public StepTypeBuilder DependsOn<TStep>() where TStep : IStepProcessor
    {
        Dependencies.Add(typeof(TStep));
        return this;
    }

    public StepTypeBuilder Internal()
    {
        ExecutionKind = StepExecutionKind.Internal;
        CompensationType = null;
        return this;
    }

    public StepTypeBuilder External()
    {
        ExecutionKind = StepExecutionKind.External;
        return this;
    }

    public StepTypeBuilder WithCompensation<TCompensation>() where TCompensation : ICompensationStep
    {
        CompensationType = typeof(TCompensation);
        return this;
    }
}