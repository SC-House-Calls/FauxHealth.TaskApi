using FauxHealth.Backend.StepsPipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend.TasksPipeline;

public sealed class SagaTaskProcessor<TRequest, TPayload>(
    TaskDefinitionRegistry registry,
    ILogger<SagaTaskProcessor<TRequest, TPayload>> logger,
    Func<TaskDelegate, TaskPipeline> taskPipelineFactory,
    Func<StepDelegate, StepPipeline> stepPipelineFactory,
    IFinalPayloadFactory<TRequest, TPayload> payloadFactory)
    : ITaskProcessor<TRequest>
    where TRequest : TaskRequest where TPayload : notnull
{
    public Task<TaskResponse> ProcessAsync(TaskContext<TRequest> context, CancellationToken cancellationToken)
    {
        TaskDelegate terminal = _ => ExecuteAsync(context, cancellationToken);
        var pipeline = taskPipelineFactory(terminal);
        return pipeline.Invoke(context);
    }

    private async Task<TaskResponse> ExecuteAsync(TaskContext<TRequest> ctx, CancellationToken ct)
    {
        var def = registry.GetDefinition<TRequest>()
                  ?? throw new InvalidOperationException($"No definition for {typeof(TRequest).Name}");

        var executed = new HashSet<Type>();
        var remaining = new Dictionary<Type, StepNode>(def.Steps);
        var queue = new Queue<StepNode>(remaining.Values.Where(s => s.Dependencies.Count == 0));
        foreach (var seed in queue.Select(s => s.StepProcessorType).ToList()) remaining.Remove(seed);

        var compStack = new Stack<ICompensationStep>();

        var dbContext = ctx.ServiceProvider.GetRequiredService<DbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                var stepId = CorrelationId.NewId();
                var stepCtx = new StepExecutionContext(ctx, node, stepId);

                StepDelegate terminalStep = async _ =>
                {
                    var step = (IStepProcessor)ctx.ServiceProvider.GetRequiredService(node.StepProcessorType);
                    return await step.ProcessAsync(ctx);
                };

                var stepPipeline = stepPipelineFactory(terminalStep);
                var result = await stepPipeline.Invoke(stepCtx);

                if (result.IsError)
                {
                    await CompensateAsync(compStack, ctx);
                    return new TaskResponse.Failure(ctx.CorrelationId, ctx.Request.CreatedBy, result.FirstError);
                }

                executed.Add(node.StepProcessorType);

                if (node.CompensationStepType is not null)
                {
                    var comp = (ICompensationStep)ctx.ServiceProvider.GetRequiredService(node.CompensationStepType);
                    compStack.Push(comp);
                }

                var unblocked = remaining.Values.Where(s => s.Dependencies.IsSubsetOf(executed)).ToList();
                foreach (var s in unblocked)
                {
                    queue.Enqueue(s);
                    remaining.Remove(s.StepProcessorType);
                }
            }

            var payload = await payloadFactory.CreateAsync(ctx, ct);
            logger.LogInformation("Task {TaskId} completed successfully", ctx.CorrelationId);
            await transaction.CommitAsync(ct);
            return new TaskResponse.Success<TPayload>(ctx.CorrelationId, ctx.Request.CreatedBy, payload);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task CompensateAsync(Stack<ICompensationStep> stack, ITaskContext context)
    {
        while (stack.Count > 0)
        {
            try { await stack.Pop().CompensateAsync(context); }
            catch { /* swallow or log; compensations are best-effort */ }
        }
    }

    async Task<TaskResponse> ITaskProcessor.ProcessAsync(ITaskContext context, CancellationToken cancellationToken)
    {
        return await ProcessAsync((TaskContext<TRequest>)context, cancellationToken);   
    }
}