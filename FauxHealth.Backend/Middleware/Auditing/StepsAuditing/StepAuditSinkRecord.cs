using ErrorOr;

namespace FauxHealth.Backend.Middleware.Auditing.StepsAuditing;

public abstract record StepAuditSinkRecord : AuditSinkRecord
{
    public CorrelationId StepId { get; }
    public string Step { get; }

    private StepAuditSinkRecord(CorrelationId taskId, CorrelationId stepId, string step) : base("StepAudit", taskId)
    {
        StepId = stepId;
        Step = step;
    }

    public sealed record StepAuditStartedSinkRecord : StepAuditSinkRecord
    {
        public DateTimeOffset StartedAt { get; }

        public StepAuditStartedSinkRecord(CorrelationId taskId, CorrelationId stepId, string step, DateTimeOffset startedAt) : base(taskId, stepId, step)
        {
            StartedAt = startedAt;
        }
    }

    public sealed record StepAuditCompletedSinkRecord : StepAuditSinkRecord
    {
        public string Outcome { get; }
        public Error? Error { get; }
        public DateTimeOffset CompletedAt { get; }
        public double DurationMs { get; }
        
        public StepAuditCompletedSinkRecord(CorrelationId taskId, CorrelationId stepId, string step, string outcome, 
            Error? error, DateTimeOffset completedAt, double durationMs) : base(taskId, stepId, step)
        {
            Outcome = outcome;
            Error = error;
            CompletedAt = completedAt;
            DurationMs = durationMs;
        }
    }
}