using System.Text.Json;
using FastEndpoints;
using FauxHealth.Backend;
using FauxHealth.Backend.Middleware.Logging;
using SpanExtensions;

namespace FauxHealth.TaskApi;

public sealed class TaskEventsSseEndpoint(ITaskEventBus eventBus)
    : EndpointWithoutRequest
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };
    public override void Configure()
    {
        Get("/tasks/{TaskId:guid}/events");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var taskId = CorrelationId.FromGuid(Route<Guid>("TaskId"));

        // Parse optional filter query: ?types=Log,TaskAudit
        var typesParam = Query<string>("types").AsSpan();
        HashSet<TaskEventType>? filter = null;
        if (!typesParam.IsEmpty)
        {
            filter = [];
            foreach (var typeParam in ReadOnlySpanExtensions.Split(typesParam, ',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (TaskEventTypeExtensions.TryParse(typeParam, out var taskEventType, true))
                    filter.Add(taskEventType);
            }
        }

        HttpContext.Response.Headers.ContentType = "text/event-stream";
        HttpContext.Response.Headers.CacheControl = "no-cache";
        HttpContext.Response.Headers.Connection = "keep-alive";

        await foreach (var evt in eventBus.Subscribe(taskId, ct))
        {
            if (filter is not null && !filter.Contains(evt.EventType))
                continue;

            var json = JsonSerializer.Serialize(evt, JsonSerializerOptions);
            await HttpContext.Response.WriteAsync($"event: {evt.EventType}\n", ct);
            await HttpContext.Response.WriteAsync($"data: {json}\n\n", ct);
            await HttpContext.Response.Body.FlushAsync(ct);
        }
    }
}