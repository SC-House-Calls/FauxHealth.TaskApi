using System.Data;

namespace FauxHealth.Backend;

public readonly struct CorrelationId
{
    private readonly Guid _id;
    private CorrelationId(Guid id) => _id = id;
    public CorrelationId() => _id = Guid.Empty;
 
    public static CorrelationId NewId() => new CorrelationId(Guid.CreateVersion7(DateTimeOffset.UtcNow));
    public static readonly CorrelationId Empty = new CorrelationId();
    public static CorrelationId FromGuid(Guid id) => new CorrelationId(id);

    public static implicit operator Guid(CorrelationId id) => id._id;
}

public abstract class AuditRecord;

public sealed class TaskAudit : AuditRecord
{
    public Guid Id { get; }
    public CorrelationId TaskId { get; }
    public DateTimeOffset ChangedAt { get; }
    public Guid ChangedBy { get; }
    public StatusEnum Status { get; }
    public string TaskName { get; }

    public TaskAudit(CorrelationId taskId, Guid changedBy, StatusEnum status, Type taskType)
    {
        TaskId = taskId;
        ChangedBy = changedBy;
        Status = status;
        Id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
        ChangedAt = DateTimeOffset.UtcNow;
        TaskName = taskType.Name;
    }
}

public sealed class StepAudit : AuditRecord
{
    public Guid Id { get; }
    public CorrelationId TaskId { get; }
    public CorrelationId StepId { get; }
    public DateTimeOffset ChangedAt { get; }
    public Guid ChangedBy { get; }
    public StatusEnum Status { get; }
    public string StepName { get; }
    
    public StepAudit(CorrelationId taskId, CorrelationId stepId, Guid changedBy, StatusEnum status, Type stepType)
    {
        TaskId = taskId;
        StepId = stepId;
        ChangedBy = changedBy;
        Status = status;
        Id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
        ChangedAt = DateTimeOffset.UtcNow;
        StepName = stepType.Name;
    }
}

public sealed class FieldChangeAudit : AuditRecord
{
    public FieldChangeAudit(CorrelationId taskId, CorrelationId stepId, Guid changedBy, string fieldName, string oldValue, string newValue, SqlDbType sqlDbType)
    {
        TaskId = taskId;
        StepId = stepId;
        ChangedBy = changedBy;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
        SqlDbType = sqlDbType;
        Id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
        ChangedAt = DateTimeOffset.UtcNow;
        
    }

    public Guid Id { get; }
    public CorrelationId TaskId { get; }
    public CorrelationId StepId { get; }
    public DateTimeOffset ChangedAt { get; }
    public Guid ChangedBy { get; }
    public string FieldName { get; }
    public string OldValue { get; }
    public string NewValue { get; }
    public SqlDbType SqlDbType { get; }
}