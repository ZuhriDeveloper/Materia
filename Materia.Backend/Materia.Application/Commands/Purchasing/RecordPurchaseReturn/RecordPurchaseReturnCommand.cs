namespace Materia.Application.Commands.Purchasing.RecordPurchaseReturn;

public record RecordPurchaseReturnCommand(
    Guid PurchaseOrderId,
    IReadOnlyList<ReturnPurchaseOrderLineInput> Lines,
    string Reason,
    string ReturnedBy);

public record ReturnPurchaseOrderLineInput(Guid ProductId, decimal ReturnedQty);
