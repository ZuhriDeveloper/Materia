namespace Materia.Application.DTOs.Stores;

public record StoreDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive);
