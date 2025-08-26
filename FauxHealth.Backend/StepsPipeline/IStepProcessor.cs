using ErrorOr;
using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend.StepsPipeline;

public interface IStepProcessor
{
    Task<ErrorOr<Success>> ProcessAsync(ITaskContext context);
}