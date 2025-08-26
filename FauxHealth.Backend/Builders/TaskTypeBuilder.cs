using System.Collections.Frozen;
using FauxHealth.Backend.StepsPipeline;
using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend.Builders;

public sealed class TaskTypeBuilder<TRequest> where TRequest : TaskRequest
{
    private readonly List<StepTypeBuilder> _steps = [];
    private Type? _payloadType;
    private Type? _customProcessorType;

    public StepTypeBuilder Step<TStep>() where TStep : IStepProcessor
    {
        var b = new StepTypeBuilder(typeof(TStep));
        _steps.Add(b);
        return b;
    }

    // Use generic Saga processor with a final payload type
    public TaskTypeBuilder<TRequest> UseSaga<TPayload>() where TPayload : notnull
    {
        _payloadType = typeof(TPayload);
        _customProcessorType = null;
        return this;
    }

    // Or specify a custom concrete processor that implements ITaskProcessor<TRequest>
    public TaskTypeBuilder<TRequest> UseProcessor<TProcessor>()
        where TProcessor : class, ITaskProcessor<TRequest>
    {
        _customProcessorType = typeof(TProcessor);
        _payloadType = null;
        return this;
    }

    internal (TaskDefinition Definition, Type? PayloadType, Type? ProcessorType) Build()
    {
        var nodes = _steps
            .GroupBy(s => s.ProcessorType).Select(g => g.Last())
            .Select(s => new StepNode(s.ProcessorType, new HashSet<Type>(s.Dependencies)) { CompensationStepType = s.CompensationType })
            .ToList();

        var definition = new TaskDefinition(nodes)
        {
            ExecutionKinds = nodes.ToDictionary(
                n => n.StepProcessorType,
                n => _steps.Last(s => s.ProcessorType == n.StepProcessorType).ExecutionKind
            ).ToFrozenDictionary()
        };

        return (definition, _payloadType, _customProcessorType);
    }
}