namespace Materia.Application.DTOs.Inventory;

public record StockDto(
    Guid ProductId,
    decimal Quantity,
    string Unit,
    DateTime? LastAdjustedAt,
    string? LastAdjustedBy,
    Guid? VariantId = null,
    string? ColorName = null);
