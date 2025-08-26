using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend.StepsPipeline;

public sealed record StepExecutionContext(ITaskContext TaskContext, StepNode StepNode, CorrelationId StepCorrelationId);