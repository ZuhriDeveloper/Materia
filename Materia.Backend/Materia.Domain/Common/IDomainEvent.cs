namespace Materia.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
