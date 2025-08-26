using System.Text.Json.Serialization;

namespace FauxHealth.Backend.TasksPipeline;


[JsonSerializable(typeof(TaskRequest))]
public partial class TaskRequestSerializationContext : JsonSerializerContext;