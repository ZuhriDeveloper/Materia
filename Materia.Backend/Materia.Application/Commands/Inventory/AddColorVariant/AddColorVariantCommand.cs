namespace Materia.Application.Commands.Inventory.AddColorVariant;

public record AddColorVariantCommand(
    Guid ProductId,
    string ColorName,
    string? ColorCode,
    string? Barcode,
    decimal? PriceOverride,
    string UpdatedBy);
