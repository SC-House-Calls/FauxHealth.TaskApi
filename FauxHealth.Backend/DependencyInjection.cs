using System.Reflection;
using System.Threading.Channels;
using FauxHealth.Backend.Middleware.Logging;
using FauxHealth.Backend.StepsPipeline;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend;

public static class DependencyInjection
{
    public static IServiceCollection AddTaskApi(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton(_ => Channel.CreateUnbounded<ITaskContext>());
        services.AddSingleton(sp => sp.GetRequiredService<Channel<ITaskContext>>().Reader);
        services.AddSingleton(sp => sp.GetRequiredService<Channel<ITaskContext>>().Writer);
        services.AddHostedService<JobSystemService>();

        services.AddTaskEngineFromAssemblies(assemblies);

        return services;
    }

    public static void AddRealTimeObservability(this IHostApplicationBuilder builder, IServiceProvider sp) =>
        builder.Logging.AddProvider(new TaskEventLoggerProvider(sp.GetRequiredService<ITaskEventBus>(),
            sp.GetRequiredService<ICorrelationAccessor>()));
}