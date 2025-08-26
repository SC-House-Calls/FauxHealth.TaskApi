using System.Threading.Channels;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FauxHealth.Backend;

public class JobSystemService : BackgroundService
{
    private readonly ChannelReader<ITaskContext> _channelReader;

    public JobSystemService(ChannelReader<ITaskContext> channelReader)
    {
        _channelReader = channelReader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var context in _channelReader.ReadAllAsync(stoppingToken))
        {
            _ = Task.Run(async () =>
            {
                await using (context)
                {
                    var requestType = context.Request.GetType();
                    var processorType = typeof(ITaskProcessor<>).MakeGenericType(requestType);
                    var processor = context.ServiceProvider.GetRequiredService(processorType);

                    dynamic dynProcessor = processor;
                    TaskResponse response = await dynProcessor.ProcessAsync((dynamic)context, stoppingToken);
                    // TODO: persist status/result, notify, etc.
                }
            }, stoppingToken);
        }
    }
}