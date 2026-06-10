namespace Materia.Application.DTOs.Stores;

/// <summary>
/// Internal DTO carrying store identity + optional parameters fields.
/// Used by GetMyStoreQueryHandler to compose the StoreProfileDto.
/// </summary>
public record StoreParametersDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    string? Address,
    string? Phone,
    decimal? MaxDeliveryDistanceKm);
