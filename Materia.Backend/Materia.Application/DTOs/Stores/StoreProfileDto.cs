namespace Materia.Application.DTOs.Stores;

/// <summary>
/// Richer projection returned by GET api/store/profile. Includes optional parameters
/// and a flag indicating whether a logo has been uploaded.
/// </summary>
public record StoreProfileDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    string? Address,
    string? Phone,
    decimal? MaxDeliveryDistanceKm,
    bool HasLogo);
