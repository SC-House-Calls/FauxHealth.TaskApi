namespace FauxHealth.Backend;

public sealed record Status(StatusEnum StatusEnum, string Details, DateTimeOffset LastUpdated);