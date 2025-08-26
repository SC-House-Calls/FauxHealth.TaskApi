using Microsoft.Extensions.Logging;

namespace FauxHealth.Backend.Middleware.Auditing;

public interface IAuditSink
{
    Task WriteAsync(AuditSinkRecord auditRecord, CancellationToken ct);
}

public sealed class NullAuditSink : IAuditSink
{
    public Task WriteAsync(AuditSinkRecord auditRecord, CancellationToken ct) => Task.CompletedTask;
}

public sealed class ConsoleAuditSink(ILogger<ConsoleAuditSink> logger) : IAuditSink
{
    public Task WriteAsync(AuditSinkRecord auditRecord, CancellationToken ct)
    {
        logger.LogInformation("{@Audit}", auditRecord);
        return Task.CompletedTask;
    }
}