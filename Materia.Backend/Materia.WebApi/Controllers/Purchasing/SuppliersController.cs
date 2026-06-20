using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Purchasing.RegisterSupplier;
using Materia.Application.Commands.Purchasing.SetPurchasePrice;
using Materia.Application.Commands.Purchasing.SetSupplierStatus;
using Materia.Application.Commands.Purchasing.UpdateSupplier;
using Materia.Application.Queries.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Purchasing;

[ApiController]
[Authorize(Roles = "Admin,Gudang")]
[Route("api/suppliers")]
public class SuppliersController(
    RegisterSupplierCommandHandler        registerHandler,
    UpdateSupplierCommandHandler          updateHandler,
    SetSupplierStatusCommandHandler       statusHandler,
    SetPurchasePriceCommandHandler        priceHandler,
    GetSuppliersPagedQueryHandler         getSuppliersPagedHandler,
    GetSupplierByIdQueryHandler           getByIdHandler,
    IValidator<RegisterSupplierCommand>   registerValidator,
    IValidator<UpdateSupplierCommand>     updateValidator,
    IValidator<SetPurchasePriceCommand>   priceValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search     = null,
        [FromQuery] bool    activeOnly = false,
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 20,
        CancellationToken ct = default)
    {
        var result = await getSuppliersPagedHandler.HandleAsync(
            new GetSuppliersPagedQuery(search, activeOnly, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await getByIdHandler.HandleAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterSupplierRequest request, CancellationToken ct)
    {
        var command    = new RegisterSupplierCommand(
            request.Name, request.ContactPhone, request.Description,
            request.SalesmanName, request.SalesmanPhone, CurrentUser);
        var validation = await registerValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await registerHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var command    = new UpdateSupplierCommand(
            id, request.Name, request.ContactPhone, request.Description,
            request.SalesmanName, request.SalesmanPhone, CurrentUser);
        var validation = await updateValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await updateHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        await statusHandler.HandleAsync(new SetSupplierStatusCommand(id, request.IsActive, CurrentUser), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/prices")]
    public async Task<IActionResult> SetPrice(
        Guid id, [FromBody] SetPurchasePriceRequest request, CancellationToken ct)
    {
        var command = new SetPurchasePriceCommand(
            id, request.ProductId, request.Amount, request.Currency,
            request.Unit, request.EffectiveFrom ?? DateTime.UtcNow, CurrentUser);

        var validation = await priceValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await priceHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record RegisterSupplierRequest(
    string Name, string? ContactPhone, string? Description,
    string? SalesmanName, string? SalesmanPhone);
public record UpdateSupplierRequest(
    string Name, string? ContactPhone, string? Description,
    string? SalesmanName, string? SalesmanPhone);
public record SetStatusRequest(bool IsActive);
public record SetPurchasePriceRequest(
    Guid      ProductId,
    decimal   Amount,
    string    Currency,
    string    Unit,
    DateTime? EffectiveFrom);
