namespace Materia.Application.Commands.Auth;

/// <summary>Replaces the full set of roles assigned to a store-scoped user.</summary>
public record UpdateUserRolesCommand(string UserId, IReadOnlyList<string> Roles);
