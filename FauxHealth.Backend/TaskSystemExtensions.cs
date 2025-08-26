using System.Collections.Frozen;
using System.Reflection;
using FauxHealth.Backend.Middleware;
using FauxHealth.Backend.Middleware.Auditing;
using FauxHealth.Backend.Middleware.Logging;
using FauxHealth.Backend.StepsPipeline;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.Extensions.DependencyInjection;

namespace FauxHealth.Backend;

internal static class TaskSystemExtensions
{
    internal static IServiceCollection AddTaskEngineFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Steps and compensations
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo<IStepProcessor>()).AsSelf().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo<ICompensationStep>()).AsSelf().WithScopedLifetime()
            .AddClasses(c => c.Where(t => HasOpenGeneric(t, typeof(IDomainUpdateAuditor<>)))).AsImplementedInterfaces().WithScopedLifetime()
        );

        services.AddSingleton<ITaskEventBus, InMemoryTaskEventBus>();
        services.AddSingleton<ICorrelationAccessor, CorrelationAccessor>();
        services.AddSingleton<IUpdateCollector, UpdateCollector>();
        services.AddSingleton<IUpdateCollectorAccessor, UpdateCollectorAccessor>();
        services.AddSingleton<DomainUpdateAuditDispatcher>();
        services.AddSingleton<IAuditSink, ConsoleAuditSink>();
        
        services.AddScoped<ITaskMiddleware, TaskAuditingMiddleware>();
        services.AddScoped<IStepMiddleware, StepAuditingMiddleware>();
        services.AddScoped<IStepMiddleware, InternalStepTransactionMiddleware>();

        services.AddScoped<Func<TaskDelegate, TaskPipeline>>(sp => terminal => new TaskPipeline(sp.GetServices<ITaskMiddleware>(), terminal));
        services.AddScoped<Func<StepDelegate, StepPipeline>>(sp => terminal => new StepPipeline(sp.GetServices<IStepMiddleware>(), terminal));
        services.AddSingleton<TaskContextFactory>();

        // Scan configurations and build registries; register processors
        var configs = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ITaskConfiguration).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ToArray();

        var defMap = new Dictionary<Type, TaskDefinition>();
        var spProvider = services.BuildServiceProvider(); // safe for configuration time

        foreach (var cfgType in configs)
        {
            var cfg = (ITaskConfiguration)ActivatorUtilities.CreateInstance(spProvider, cfgType);
            var builderType = typeof(TaskTypeBuilder<>).MakeGenericType(cfg.TaskRequestType);
            dynamic builder = Activator.CreateInstance(builderType)!;
            cfg.Configure(builder);
            var built = ((object Definition, Type? PayloadType, Type? ProcessorType))builder.Build();

            defMap[cfg.TaskRequestType] = (TaskDefinition)built.Definition;

            // Register processor
            var itp = typeof(ITaskProcessor<>).MakeGenericType(cfg.TaskRequestType);
            if (built.ProcessorType is { } custom)
            {
                services.AddScoped(itp, custom);
            }
            else if (built.PayloadType is { } payloadType)
            {
                var sagaType = typeof(SagaTaskProcessor<,>).MakeGenericType(cfg.TaskRequestType, payloadType);
                // Also register a payload factory; require one to be available
                var factoryType = typeof(IFinalPayloadFactory<,>).MakeGenericType(cfg.TaskRequestType, payloadType);
                // If none registered yet, throw later at runtime; or register a no-op factory placeholder as needed.
                services.AddScoped(itp, sagaType);
            }
            else
            {
                throw new InvalidOperationException($"Task '{cfg.TaskRequestType.Name}' must specify UseSaga<TPayload>() or UseProcessor<TProcessor>().");
            }
        }

        services.AddSingleton(new TaskDefinitionRegistry(defMap.ToFrozenDictionary()));

        return services;
    }

    private static bool HasOpenGeneric(Type type, Type openGeneric)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
}