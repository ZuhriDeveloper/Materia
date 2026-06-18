using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductBaseUnitChanged(
    ProductId ProductId,
    string BaseUnit,
    string ChangedBy,
    DateTime OccurredAt) : IDomainEvent;
