namespace Materia.Application.Commands.Purchasing.ClosePurchaseOrder;

public record ClosePurchaseOrderCommand(
    Guid PurchaseOrderId,
    string Reason,
    string ClosedBy);
