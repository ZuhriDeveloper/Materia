using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Purchasing.CancelPurchaseOrder;
using Materia.Application.Commands.Purchasing.ConfirmPurchaseOrder;
using Materia.Application.Commands.Purchasing.CreatePurchaseOrder;
using Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;
using Materia.Application.Queries.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Purchasing;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/purchase-orders")]
public class PurchaseOrdersController(
    CreatePurchaseOrderCommandHandler        createHandler,
    ConfirmPurchaseOrderCommandHandler       confirmHandler,
    ReceivePurchaseOrderCommandHandler       receiveHandler,
    CancelPurchaseOrderCommandHandler        cancelHandler,
    GetPurchaseOrdersQueryHandler            getAllHandler,
    GetPurchaseOrderByIdQueryHandler         getByIdHandler,
    IValidator<CreatePurchaseOrderCommand>   createValidator,
    IValidator<ReceivePurchaseOrderCommand>  receiveValidator,
    IValidator<CancelPurchaseOrderCommand>   cancelValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var result = await getAllHandler.HandleAsync(ct);
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
    public async Task<IActionResult> Create(
        [FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new CreatePurchaseOrderCommand(request.SupplierId, request.Lines, CurrentUser);

        var validation = await createValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await confirmHandler.HandleAsync(new ConfirmPurchaseOrderCommand(id, CurrentUser), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(
        Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new ReceivePurchaseOrderCommand(id, request.Lines, CurrentUser);

        var validation = await receiveValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await receiveHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelPurchaseOrderRequest request, CancellationToken ct)
    {
        var command = new CancelPurchaseOrderCommand(id, request.Reason, CurrentUser);

        var validation = await cancelValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await cancelHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    IReadOnlyList<CreatePurchaseOrderLineInput> Lines);

public record ReceivePurchaseOrderRequest(
    IReadOnlyList<ReceivePurchaseOrderLineInput> Lines);

public record CancelPurchaseOrderRequest(string Reason);
