namespace Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;

public record ReceivePurchaseOrderCommand(
    Guid PurchaseOrderId,
    IReadOnlyList<ReceivePurchaseOrderLineInput> Lines,
    string ReceivedBy);

public record ReceivePurchaseOrderLineInput(Guid ProductId, decimal ReceivedQty, Guid? VariantId = null);
