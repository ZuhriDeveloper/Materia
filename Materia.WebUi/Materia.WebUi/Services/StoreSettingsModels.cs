namespace Materia.WebUi.Services;

public record StoreProfileDto(
    Guid    Id,
    string  Name,
    string  Code,
    bool    IsActive,
    string? Address,
    string? Phone,
    double? MaxDeliveryDistanceKm,
    bool    HasLogo);
