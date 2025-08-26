namespace FauxHealth.Backend.Middleware.Auditing;

public abstract record AuditSinkRecord
{
    public string Type { get; }
    public CorrelationId TaskId { get; }

    protected AuditSinkRecord(string type, CorrelationId taskId)
    {
        Type = type;
        TaskId = taskId;
    }
}