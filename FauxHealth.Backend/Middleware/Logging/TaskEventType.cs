namespace FauxHealth.Backend.Middleware.Logging;

public enum TaskEventType : byte
{
    StatusChanged = 0,
    Log = 1,
    TaskAudit = 2,
    StepAudit = 3,
    FieldChangeAudit = 4
}