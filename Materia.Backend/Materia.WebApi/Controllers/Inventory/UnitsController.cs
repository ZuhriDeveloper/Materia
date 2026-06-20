using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Inventory.CreateUnit;
using Materia.Application.Commands.Inventory.SetUnitStatus;
using Materia.Application.Commands.Inventory.UpdateUnit;
using Materia.Application.Queries.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Inventory;

[ApiController]
[Authorize(Roles = "Admin,Gudang")]
[Route("api/[controller]")]
public class UnitsController(
    CreateUnitCommandHandler createHandler,
    UpdateUnitCommandHandler updateHandler,
    SetUnitStatusCommandHandler statusHandler,
    GetUnitsQueryHandler getHandler,
    IValidator<CreateUnitCommand> createValidator,
    IValidator<UpdateUnitCommand> updateValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(new GetUnitsQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var command = new CreateUnitCommand(request.Name, request.Symbol, CurrentUser);
        var validation = await createValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await createHandler.HandleAsync(command, ct);
        return Ok(new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
    {
        var command = new UpdateUnitCommand(id, request.Name, request.Symbol, CurrentUser);
        var validation = await updateValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await updateHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetUnitStatusRequest request, CancellationToken ct)
    {
        await statusHandler.HandleAsync(new SetUnitStatusCommand(id, request.IsActive, CurrentUser), ct);
        return NoContent();
    }
}

public record CreateUnitRequest(string Name, string? Symbol);
public record UpdateUnitRequest(string Name, string? Symbol);
public record SetUnitStatusRequest(bool IsActive);
