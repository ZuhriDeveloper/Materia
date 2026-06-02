using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Sales.FinalizeSale;
using Materia.Application.DTOs.Sales;
using Materia.Application.Services;
using Materia.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Admin,Cashier")]
[Route("api/[controller]")]
public class SalesController(
    SaleService                       saleService,
    FinalizeSaleCommandHandler        finalizeHandler,
    IValidator<FinalizeSaleCommand>   finalizeValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int         page     = 1,
        [FromQuery] int         pageSize = 20,
        [FromQuery] SaleStatus? status   = null,
        [FromQuery] DateTime?   from     = null,
        [FromQuery] DateTime?   to       = null,
        CancellationToken ct = default)
    {
        var result = await saleService.GetPagedAsync(page, pageSize, status, from, to, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await saleService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        var id = await saleService.CreateSaleAsync(request, CurrentUser, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(
        Guid id, [FromBody] AddSaleItemRequest request, CancellationToken ct)
    {
        var itemId = await saleService.AddItemAsync(id, request, CurrentUser, ct);
        return Ok(new { itemId });
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid id, Guid itemId, CancellationToken ct)
    {
        await saleService.RemoveItemAsync(id, itemId, CurrentUser, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(
        Guid id, [FromBody] CheckoutRequest request, CancellationToken ct)
    {
        await saleService.CheckoutAsync(id, request, CurrentUser, ct);
        var sale = await saleService.GetByIdAsync(id, ct);
        return Ok(sale);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelSaleRequest request, CancellationToken ct)
    {
        await saleService.CancelAsync(id, request.Reason, CurrentUser, ct);
        return NoContent();
    }

    // ── Consumer sale (single-step finalize) ────────────────────────────────────

    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize(
        [FromBody] FinalizeSaleRequest request, CancellationToken ct)
    {
        var command = new FinalizeSaleCommand(
            request.CustomerId,
            request.CustomerName,
            request.Items,
            request.IsDeliveryRequired,
            CurrentUser);

        var validation = await finalizeValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await finalizeHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.SaleId }, result);
    }
}

public record FinalizeSaleRequest(
    Guid?                                CustomerId,
    string?                              CustomerName,
    IReadOnlyList<FinalizeSaleItemInput> Items,
    bool                                 IsDeliveryRequired);
