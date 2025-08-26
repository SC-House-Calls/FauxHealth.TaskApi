using FauxHealth.Backend.StepsPipeline;
using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend.Middleware.Logging;

public sealed class TaskEventLoggerProvider(
    ITaskEventBus eventBus,
    ICorrelationAccessor correlationAccessor) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new TaskEventLogger(categoryName, eventBus, correlationAccessor);

    public void Dispose() { }

    private sealed class TaskEventLogger(
        string categoryName,
        ITaskEventBus eventBus,
        ICorrelationAccessor correlationAccessor) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var taskId = correlationAccessor.TaskId;
            if (taskId is null) return; // Only publish logs when inside a task

            var message = formatter(state, exception);
            var payload = new
            {
                Level = logLevel.ToString(),
                Category = categoryName,
                Message = message,
                Exception = exception?.ToString()
            };

            eventBus.Publish(new TaskEvent(TaskEventType.Log, taskId.Value, DateTimeOffset.UtcNow, payload));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}