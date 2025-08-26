using System.Text.Json;
using FauxHealth.Backend.StepsPipeline;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend.Outbox;

public sealed class OutboxProcessorService(IServiceProvider root,
    ITaskRequestSerializationRegistry registry,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = root.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbContext>();

            var messages = await db.Set<OutboxMessage>()
                .Where(m => !m.Processed)
                .OrderBy(m => m.CreatedAt)
                .Take(50)
                .ToListAsync(stoppingToken);
            
            foreach (var msg in messages)
            {
                try
                {
                    var stepType = Type.GetType(msg.StepProcessorType)!;
                    var step = (IStepProcessor)scope.ServiceProvider.GetRequiredService(stepType);

                    // Determine request type from the step's generic argument or registry mapping
                    var requestType = stepType.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStepProcessor))
                        .GetGenericArguments()[0];

                    var typeInfo = registry.GetTypeInfo(requestType);
                    var request = (TaskRequest)JsonSerializer.Deserialize(msg.RequestData, typeInfo)!;

                    var ctxFactory = scope.ServiceProvider.GetRequiredService<TaskContextFactory>();
                    await using var ctx = ctxFactory.Create(request, stoppingToken);
                    ctx.CorrelationId = CorrelationId.FromGuid(msg.TaskId);

                    var result = await step.ProcessAsync(ctx);
                    if (result.IsError)
                    {
                        msg.Error = result.FirstError.Description;
                        logger.LogWarning("External step {Step} failed for Task {TaskId}", stepType.Name, msg.TaskId);
                    }
                    else
                    {
                        msg.Processed = true;
                        msg.ProcessedAt = DateTimeOffset.UtcNow;
                        logger.LogInformation("External step {Step} processed for Task {TaskId}", stepType.Name, msg.TaskId);
                    }
                }
                catch (Exception ex)
                {
                    msg.Error = ex.Message;
                    logger.LogError(ex, "Error processing outbox message {Id}", msg.Id);
                }
            }

            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        }
    }
}