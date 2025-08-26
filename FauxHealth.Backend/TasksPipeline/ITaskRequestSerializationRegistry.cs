using System.Collections.Frozen;
using System.Text.Json.Serialization.Metadata;

namespace FauxHealth.Backend.TasksPipeline;

public interface ITaskRequestSerializationRegistry
{
    JsonTypeInfo GetTypeInfo(Type requestType);
}

public sealed class TaskRequestSerializationRegistry : ITaskRequestSerializationRegistry
{
    private readonly FrozenDictionary<Type, JsonTypeInfo> _map;

    public TaskRequestSerializationRegistry(TaskRequestSerializationContext ctx)
    {
        _map = new Dictionary<Type, JsonTypeInfo>
        {
            /*{ typeof(UpdateEmployeeTask), ctx.UpdateEmployeeTask },
            { typeof(CreateUserTask), ctx.CreateUserTask },
            { typeof(AnotherTaskType), ctx.AnotherTaskType }*/
        }.ToFrozenDictionary();
    }

    public JsonTypeInfo GetTypeInfo(Type requestType)
    {
        return !_map.TryGetValue(requestType, out var info) 
            ? throw new InvalidOperationException($"No JsonTypeInfo registered for {requestType.Name}") 
            : info;
    }
}