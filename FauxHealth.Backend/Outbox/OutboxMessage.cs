namespace FauxHealth.Backend.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);
    public Guid TaskId { get; init; }
    public Guid StepId { get; init; }
    public Guid CreatedBy { get; init; }
    public string StepProcessorType { get; init; } = string.Empty;
    public byte[] RequestData { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool Processed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
}