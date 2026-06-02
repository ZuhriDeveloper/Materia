namespace Materia.Application.Commands.Purchasing.ConfirmPurchaseOrder;

public record ConfirmPurchaseOrderCommand(Guid PurchaseOrderId, string ConfirmedBy);
