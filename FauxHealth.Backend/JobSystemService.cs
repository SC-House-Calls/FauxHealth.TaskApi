using System.Threading.Channels;
using FauxHealth.Backend.TasksPipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend;

public class JobSystemService : BackgroundService
{
    private readonly ChannelReader<ITaskContext> _channelReader;
    private readonly ILogger<JobSystemService> _logger;

    public JobSystemService(ChannelReader<ITaskContext> channelReader, ILogger<JobSystemService> logger)
    {
        _channelReader = channelReader;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var context in _channelReader.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation("Processing task context {@context} with Id: {TaskId} at {TimeStamp}", context, (Guid)context.CorrelationId, DateTimeOffset.UtcNow);
            _ = Task.Run(async () =>
            {
                await using (context)
                {
                    var requestType = context.Request.GetType();
                    _logger.LogInformation("Processing {TaskType}", requestType.Name);
                    var processorType = typeof(ITaskProcessor<>).MakeGenericType(requestType);
                    _logger.LogInformation("Requesting {ProcessorType} from IOC", processorType.Name);
                    var processor = context.ServiceProvider.GetRequiredService(processorType);

                    if (processor is not ITaskProcessor dynProcessor) throw new InvalidOperationException();
                    _logger.LogInformation("IOC returned {ProcessorType} for {TaskType}", dynProcessor.GetType().Name, requestType.Name);
                    
                    var response = await dynProcessor.ProcessAsync(context, stoppingToken);
                    switch (response)
                    {
                        case TaskResponse.Failure failure:
                            _logger.LogError("Processing {TaskType} failed at {TimeStamp}", requestType.Name, DateTimeOffset.UtcNow);
                            _logger.LogInformation("Failure: {@failure}", failure);
                            break;
                        default:
                            _logger.LogInformation("Processing {TaskType} completed at {TimeStamp}", requestType.Name, DateTimeOffset.UtcNow);
                            _logger.LogInformation("Response: {@response}", response);
                            break;
                    }
                    
                    // TODO: persist status/result, notify, etc.
                }
            }, stoppingToken);
        }
    }
}