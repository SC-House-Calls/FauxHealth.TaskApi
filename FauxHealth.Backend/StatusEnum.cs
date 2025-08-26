namespace FauxHealth.Backend;

public enum StatusEnum : byte
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}