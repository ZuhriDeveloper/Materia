namespace Materia.Application.DTOs.Inventory;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);
