namespace Materia.Application.Commands.Purchasing.CreatePurchaseOrder;

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines,
    string CreatedBy);

public record CreatePurchaseOrderLineInput(Guid ProductId, decimal Qty);
