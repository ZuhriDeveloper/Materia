namespace Materia.Application.Commands.Purchasing.CreatePurchaseOrder;

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines,
    string CreatedBy,
    // Payment tenor (tempo). Null = cash / no tempo.
    int? PaymentTermValue = null,
    string? PaymentTermUnit = null,
    // When true, receiving goods writes each line's list price back to the supplier catalog.
    bool UpdateCatalogOnReceipt = false);

public record CreatePurchaseOrderLineInput(
    Guid ProductId,
    decimal Qty,
    // Optional chained trade discount (% per level, e.g. [12.5, 7, 5]) applied to the
    // list price for this line. Null/empty = no discount.
    IReadOnlyList<decimal>? Discounts = null,
    // Optional override of the list buy price. Null = use the supplier's catalog price.
    decimal? ListUnitCost = null);
