using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Customers.RecordReceivablePayment;
using Materia.Application.Queries.Customers;
using Materia.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Customers;

[ApiController]
[Authorize(Roles = "Admin,Cashier")]
[Route("api/[controller]")]
public sealed class ReceivablesController(
    RecordReceivablePaymentCommandHandler             recordHandler,
    GetOutstandingReceivablesQueryHandler             getHandler,
    IValidator<RecordReceivablePaymentCommand>        recordValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns customers with outstanding receivable balances (piutang),
    /// sorted by outstanding debt descending.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOutstanding(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        CancellationToken ct = default)
    {
        var result = await getHandler.HandleAsync(
            new GetOutstandingReceivablesQuery(page, pageSize, search), ct);
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Records a payment (pelunasan piutang) against a customer's open receivables.
    /// Allocates oldest invoices first (FIFO).
    /// </summary>
    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment(
        [FromBody] RecordReceivablePaymentRequest request, CancellationToken ct)
    {
        // Idempotency key: prefer the body field; fall back to the Idempotency-Key header.
        // A double-clicked or retried request must reuse the same key to be de-duplicated.
        var idempotencyKey = request.IdempotencyKey is { } bodyKey && bodyKey != Guid.Empty
            ? bodyKey
            : Guid.TryParse(Request.Headers["Idempotency-Key"], out var headerKey)
                ? headerKey
                : Guid.Empty;

        var command = new RecordReceivablePaymentCommand(
            request.CustomerId, request.Amount, request.Method,
            request.Notes, CurrentUser, idempotencyKey);

        var validation = await recordValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await recordHandler.HandleAsync(command, ct);
        return Ok(new
        {
            result.PaymentId,
            result.NewBalance,
            result.Allocations,
        });
    }
}

public record RecordReceivablePaymentRequest(
    Guid          CustomerId,
    decimal       Amount,
    PaymentMethod Method,
    string?       Notes,
    /// <summary>
    /// Client-generated de-duplication token (one per payment attempt). Optional in the body
    /// if supplied via the <c>Idempotency-Key</c> header instead; one of the two is required.
    /// </summary>
    Guid?         IdempotencyKey = null);
