namespace FauxHealth.Backend.Middleware.Auditing;

public abstract record TaskAuditSinkRecord : AuditSinkRecord
{
    private const string TaskSinkType = "TaskAudit";
    private TaskAuditSinkRecord(CorrelationId taskId, string type) : base(type, taskId) { }
    
    public sealed record TaskAuditStartedSinkRecord : TaskAuditSinkRecord
    {
        public string Request { get; }
        public Guid CreatedBy { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset StartedAt { get; }

        public TaskAuditStartedSinkRecord(CorrelationId taskId, string request, Guid createdBy, DateTimeOffset createdAt, 
            DateTimeOffset startedAt) : base(taskId,$"{TaskSinkType}Started")
        {
            Request = request;
            CreatedBy = createdBy;
            CreatedAt = createdAt;
            StartedAt = startedAt;
        }
    }

    public sealed record TaskAuditCompletedSinkRecord : TaskAuditSinkRecord
    {
        public string Result { get; }
        public DateTimeOffset CompletedAt { get; }
        public double DurationMs { get; }

        public TaskAuditCompletedSinkRecord(CorrelationId taskId, string result, DateTimeOffset completedAt, 
            double durationMs) : base(taskId, $"{TaskSinkType}Completed")
        {
            Result = result;
            CompletedAt = completedAt;
            DurationMs = durationMs;
        }
    }
    
    public sealed record TaskAuditFailedSinkRecord : TaskAuditSinkRecord
    {
        public string Error { get; }
        public DateTimeOffset FailedAt { get; }

        public TaskAuditFailedSinkRecord(CorrelationId taskId, string error, DateTimeOffset failedAt) : base(taskId, $"{TaskSinkType}Failed")
        {
            Error = error;
            FailedAt = failedAt;
        }
    }
    
}