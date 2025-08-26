using FauxHealth.Backend.Middleware.Auditing;

namespace FauxHealth.Backend.Middleware.Logging;

public sealed record TaskEvent(
    TaskEventType EventType,
    CorrelationId TaskId,
    DateTimeOffset Timestamp,
    object Payload);