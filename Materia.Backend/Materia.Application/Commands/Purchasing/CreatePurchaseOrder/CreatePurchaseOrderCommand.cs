namespace Materia.Application.Commands.Purchasing.CreatePurchaseOrder;

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines,
    string CreatedBy,
    // Payment tenor (tempo). Null = cash / no tempo.
    int? PaymentTermValue = null,
    string? PaymentTermUnit = null);

public record CreatePurchaseOrderLineInput(Guid ProductId, decimal Qty);
