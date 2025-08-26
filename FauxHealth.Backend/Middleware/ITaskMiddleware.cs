using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend.Middleware;

public interface ITaskMiddleware
{
    Task<TaskResponse> InvokeAsync(ITaskContext context, TaskDelegate next);
}