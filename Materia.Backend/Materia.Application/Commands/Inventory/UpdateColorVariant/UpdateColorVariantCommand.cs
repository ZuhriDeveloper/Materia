namespace Materia.Application.Commands.Inventory.UpdateColorVariant;

public record UpdateColorVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string ColorName,
    string? ColorCode,
    string? Barcode,
    decimal? PriceOverride,
    string UpdatedBy);
