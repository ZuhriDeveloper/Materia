using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record PurchaseOrderReceived(
    PurchaseOrderId PurchaseOrderId,
    IReadOnlyList<ReceiptLineData> Lines,
    string ReceivedBy,
    DateTime OccurredAt) : IDomainEvent;

public record ReceiptLineData(Guid ProductId, decimal ReceivedQty);
