using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Stores.RegisterStore;
using Materia.Application.Commands.Stores.RenameStore;
using Materia.Application.Commands.Stores.SetStoreStatus;
using Materia.Application.Queries.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Platform;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/platform/stores")]
public class StoresController(
    RegisterStoreCommandHandler registerHandler,
    RenameStoreCommandHandler renameHandler,
    SetStoreStatusCommandHandler statusHandler,
    GetStoresQueryHandler queryHandler,
    IValidator<RegisterStoreCommand> registerValidator,
    IValidator<RenameStoreCommand> renameValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await queryHandler.HandleAsync(new GetStoresQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await queryHandler.HandleByIdAsync(new GetStoreByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterStoreRequest request, CancellationToken ct)
    {
        var command = new RegisterStoreCommand(request.Name, request.Code, CurrentUser);
        var validation = await registerValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await registerHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}/name")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameStoreRequest request, CancellationToken ct)
    {
        var command = new RenameStoreCommand(id, request.Name, CurrentUser);
        var validation = await renameValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await renameHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStoreStatusRequest request, CancellationToken ct)
    {
        await statusHandler.HandleAsync(new SetStoreStatusCommand(id, request.IsActive, CurrentUser), ct);
        return NoContent();
    }
}

public record RegisterStoreRequest(string Name, string Code);
public record RenameStoreRequest(string Name);
public record SetStoreStatusRequest(bool IsActive);
