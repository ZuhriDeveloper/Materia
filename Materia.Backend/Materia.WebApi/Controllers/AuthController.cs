using FluentValidation;
using Materia.Application.Commands.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    LoginCommandHandler handler,
    IValidator<LoginCommand> validator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Succeeded)
            return Unauthorized(new { message = result.ErrorMessage });

        return Ok(new { result.Token, result.Email, result.FullName });
    }
}
