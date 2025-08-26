using System.Text.Json;
using FastEndpoints;
using FauxHealth.Backend;
using FauxHealth.Backend.Middleware.Logging;

public sealed class TaskEventsSseEndpoint(ITaskEventBus eventBus)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/tasks/{TaskId:guid}/events");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var taskId = CorrelationId.FromGuid(Route<Guid>("TaskId"));

        // Parse optional filter query: ?types=Log,TaskAudit
        var typesParam = Query<string>("types");
        HashSet<TaskEventType>? filter = null;
        if (!string.IsNullOrWhiteSpace(typesParam))
        {
            filter = typesParam
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<TaskEventType>(s, true, out var t) ? t : (TaskEventType?)null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToHashSet();
        }

        HttpContext.Response.Headers.ContentType = "text/event-stream";
        HttpContext.Response.Headers.CacheControl = "no-cache";
        HttpContext.Response.Headers.Connection = "keep-alive";

        await foreach (var evt in eventBus.Subscribe(taskId, ct))
        {
            if (filter is not null && !filter.Contains(evt.EventType))
                continue;

            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions { WriteIndented = false });
            await HttpContext.Response.WriteAsync($"event: {evt.EventType}\n", ct);
            await HttpContext.Response.WriteAsync($"data: {json}\n\n", ct);
            await HttpContext.Response.Body.FlushAsync(ct);
        }
    }
}