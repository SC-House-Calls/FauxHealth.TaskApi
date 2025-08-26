using ErrorOr;
using FauxHealth.Backend.StepsPipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend.Middleware;

public sealed class InternalStepTransactionMiddleware(TaskDefinitionRegistry registry, ILogger<InternalStepTransactionMiddleware> log, IServiceProvider sp)
    : IStepMiddleware
{
    public async Task<ErrorOr<Success>> InvokeAsync(StepExecutionContext ctx, StepDelegate next)
    {
        var def = registry.GetDefinition(ctx.TaskContext.Request.GetType())
                  ?? throw new InvalidOperationException("Missing task definition");

        if (!def.ExecutionKinds.TryGetValue(ctx.StepNode.StepProcessorType, out var kind) || kind != StepExecutionKind.Internal)
            return await next(ctx);

        // If no DbContext in scope, just run
        var db = sp.GetService<DbContext>();
        if (db is null) return await next(ctx);

        await using var tx = await db.Database.BeginTransactionAsync(ctx.TaskContext.CancellationToken);
        var res = await next(ctx);
        if (!res.IsError) await tx.CommitAsync(ctx.TaskContext.CancellationToken);
        else await tx.RollbackAsync(ctx.TaskContext.CancellationToken);
        return res;
    }
}