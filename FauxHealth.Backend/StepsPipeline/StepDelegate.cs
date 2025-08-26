using ErrorOr;

namespace FauxHealth.Backend.StepsPipeline;

public delegate Task<ErrorOr<Success>> StepDelegate(StepExecutionContext context);