namespace FauxHealth.Backend;

public interface IDomainUpdate;

public interface IUpdateCollector
{
    void Add(IDomainUpdate update);
    IReadOnlyList<IDomainUpdate> Drain();
}

public sealed class UpdateCollector : IUpdateCollector
{
    private readonly List<IDomainUpdate> _buffer = [];
    public void Add(IDomainUpdate update) => _buffer.Add(update);
    public IReadOnlyList<IDomainUpdate> Drain()
    {
        var copy = _buffer.ToArray();
        _buffer.Clear();
        return copy;
    }
}

// AsyncLocal access so steps can resolve a collector without new DI scopes
public interface IUpdateCollectorAccessor
{
    IUpdateCollector? Current { get; }
    IDisposable Use(IUpdateCollector collector);
}

public sealed class UpdateCollectorAccessor : IUpdateCollectorAccessor
{
    private static readonly AsyncLocal<IUpdateCollector?> _current = new();
    public IUpdateCollector? Current => _current.Value;
    public IDisposable Use(IUpdateCollector collector)
    {
        var prev = _current.Value;
        _current.Value = collector;
        return new Scope(() => _current.Value = prev);
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}