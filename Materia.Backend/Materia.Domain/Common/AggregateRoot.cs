namespace Materia.Domain.Common;

public abstract class AggregateRoot<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;
    public long Version { get; private set; }
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Called inside command methods to record a new event.</summary>
    protected void Raise(IDomainEvent domainEvent)
    {
        Apply(domainEvent);
        _domainEvents.Add(domainEvent);
        Version++;
    }

    /// <summary>Called by repositories to replay persisted events and restore state.</summary>
    public void Load(IEnumerable<IDomainEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
            Version++;
        }
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected abstract void Apply(IDomainEvent domainEvent);
}
