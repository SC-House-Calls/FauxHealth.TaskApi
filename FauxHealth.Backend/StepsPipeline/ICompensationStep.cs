using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend.StepsPipeline;

public interface ICompensationStep
{
    Task CompensateAsync(ITaskContext context);
}
