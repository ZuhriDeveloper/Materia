namespace Materia.Application.DTOs.Inventory;

public record UnitDto(
    Guid Id,
    string Name,
    string? Symbol,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);
