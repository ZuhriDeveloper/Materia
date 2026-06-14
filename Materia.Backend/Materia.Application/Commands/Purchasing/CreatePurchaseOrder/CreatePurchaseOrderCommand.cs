namespace Materia.Application.Commands.Purchasing.CreatePurchaseOrder;

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines,
    string CreatedBy,
    // Payment tenor (tempo). Null = cash / no tempo.
    int? PaymentTermValue = null,
    string? PaymentTermUnit = null);

public record CreatePurchaseOrderLineInput(
    Guid ProductId,
    decimal Qty,
    // Optional chained trade discount (% per level, e.g. [12.5, 7, 5]) applied to the
    // supplier's catalog price for this line. Null/empty = catalog price as-is.
    IReadOnlyList<decimal>? Discounts = null);
