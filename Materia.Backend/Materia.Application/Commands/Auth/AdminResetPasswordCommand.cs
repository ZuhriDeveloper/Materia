namespace Materia.Application.Commands.Auth;

/// <summary>SuperAdmin-initiated forced password reset for a store-scoped user.</summary>
public record AdminResetPasswordCommand(string UserId);
