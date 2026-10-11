namespace Materia.Application.Commands.Purchasing.SetPurchasePrice;

public record SetPurchasePriceCommand(
    Guid SupplierId,
    Guid ProductId,
    decimal Amount,
    string Currency,
    string Unit,
    DateTime EffectiveFrom,
    string UpdatedBy,
    // Optional standing chained trade discount (% per level, e.g. [12.5, 7, 5]) on the list price.
    // Null/empty = supplier grants no discount on this product.
    IReadOnlyList<decimal>? Discounts = null);
