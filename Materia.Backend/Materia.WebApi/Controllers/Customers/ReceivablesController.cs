using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Customers.RecordOpeningReceivable;
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
    RecordOpeningReceivableCommandHandler             openingHandler,
    GetOutstandingReceivablesQueryHandler             getHandler,
    IValidator<RecordReceivablePaymentCommand>        recordValidator,
    IValidator<RecordOpeningReceivableCommand>        openingValidator) : ControllerBase
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

    /// <summary>
    /// Records a pre-existing debt (saldo awal piutang) without a sale, for customers who
    /// already owed money before the application went live. Admin only: it creates debt
    /// with no goods leaving the store. Combined with the class-level attribute, so only
    /// users in the Admin role pass.
    /// </summary>
    [HttpPost("opening")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordOpening(
        [FromBody] RecordOpeningReceivableRequest request, CancellationToken ct)
    {
        var command = new RecordOpeningReceivableCommand(
            request.CustomerId, request.ReceivableId, request.Amount, request.IncurredAt,
            request.ReferenceNo, request.Notes, CurrentUser);

        var validation = await openingValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await openingHandler.HandleAsync(command, ct);
        return Ok(new
        {
            result.ReceivableId,
            result.NewBalance,
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

public record RecordOpeningReceivableRequest(
    Guid     CustomerId,
    /// <summary>Client-generated id, one per entry; reusing it is rejected as a duplicate.</summary>
    Guid     ReceivableId,
    decimal  Amount,
    DateTime IncurredAt,
    string?  ReferenceNo,
    string?  Notes);
