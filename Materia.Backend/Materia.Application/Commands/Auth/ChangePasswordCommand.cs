namespace Materia.Application.Commands.Auth;

/// <summary>Changes the password of the signed-in user. <see cref="UserId"/> comes from the JWT, not the client body.</summary>
public record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword);
