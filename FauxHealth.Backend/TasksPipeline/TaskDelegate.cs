using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend;

public delegate Task<TaskResponse> TaskDelegate(ITaskContext context);