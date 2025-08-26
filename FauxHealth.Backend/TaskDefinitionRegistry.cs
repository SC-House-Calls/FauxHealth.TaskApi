using System.Collections.Frozen;
using FauxHealth.Backend.TasksPipeline;

namespace FauxHealth.Backend;

public sealed class TaskDefinitionRegistry
{
    private readonly FrozenDictionary<Type, TaskDefinition> _definitions;
    public TaskDefinitionRegistry(FrozenDictionary<Type, TaskDefinition> definitions) => _definitions = definitions;
    public TaskDefinition? GetDefinition<TRequest>() where TRequest : TaskRequest => _definitions.GetValueOrDefault(typeof(TRequest));
    public TaskDefinition? GetDefinition(Type requestType) => _definitions.GetValueOrDefault(requestType);
}