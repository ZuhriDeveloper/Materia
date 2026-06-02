namespace Materia.Application.Commands.Purchasing.CancelPurchaseOrder;

public record CancelPurchaseOrderCommand(Guid PurchaseOrderId, string Reason, string CancelledBy);
