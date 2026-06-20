namespace Materia.WebUi.Services;

public record StoreDto(
    Guid   Id,
    string Name,
    string Code,
    bool   IsActive);

public record RegisterStoreResponse(Guid Id);

public record RegisterUserResponse(string Id);

public record UserDto(
    string       Id,
    string       Email,
    string?      FullName,
    Guid?        StoreId,
    List<string> Roles,
    bool         EmailConfirmed);
