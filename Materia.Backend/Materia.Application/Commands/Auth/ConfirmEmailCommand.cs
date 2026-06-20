namespace Materia.Application.Commands.Auth;

public record ConfirmEmailCommand(string UserId, string Token);
