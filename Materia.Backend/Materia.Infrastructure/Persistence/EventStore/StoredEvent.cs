namespace Materia.Infrastructure.Persistence.EventStore;

public class StoredEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid StoreId { get; init; }
    public string AggregateType { get; init; } = default!;
    public Guid AggregateId { get; init; }
    public long Version { get; init; }
    public string EventType { get; init; } = default!;
    public string EventData { get; init; } = default!;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
