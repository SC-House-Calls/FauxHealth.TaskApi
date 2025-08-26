using ErrorOr;
using FauxHealth.Backend.StepsPipeline;

namespace FauxHealth.Backend.Middleware;

public interface IStepMiddleware
{
    Task<ErrorOr<Success>> InvokeAsync(StepExecutionContext context, StepDelegate next);
}