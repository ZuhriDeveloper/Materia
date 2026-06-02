namespace Materia.Application.Commands.Purchasing.SetPurchasePrice;

public record SetPurchasePriceCommand(
    Guid SupplierId,
    Guid ProductId,
    decimal Amount,
    string Currency,
    string Unit,
    DateTime EffectiveFrom,
    string UpdatedBy);
