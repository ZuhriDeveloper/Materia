using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Financials.RecordPettyCashExpense;
using Materia.Application.Financials.Queries;
using Materia.Domain.Financials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Financials;

[ApiController]
[Authorize(Roles = "Admin,Cashier")]
[Route("api/[controller]")]
public sealed class PettyCashController(
    RecordPettyCashExpenseCommandHandler        recordHandler,
    GetPettyCashExpensesQueryHandler            getHandler,
    Application.Contracts.Financials.IPettyCashQueryRepository queryRepository,
    IValidator<RecordPettyCashExpenseCommand>   recordValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int                page     = 1,
        [FromQuery] int                pageSize = 20,
        [FromQuery] DateTime?          from     = null,
        [FromQuery] DateTime?          to       = null,
        [FromQuery] PettyCashCategory? category = null,
        CancellationToken ct = default)
    {
        if (from > to)
            return BadRequest(new { message = "from must not be after to." });

        var result = await getHandler.HandleAsync(
            new GetPettyCashExpensesQuery(page, pageSize, from, to, category), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await queryRepository.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Record(
        [FromBody] RecordPettyCashRequest request, CancellationToken ct)
    {
        // Idempotency key: prefer the body field; fall back to the Idempotency-Key header.
        // A double-clicked or retried request must reuse the same key to be de-duplicated.
        var idempotencyKey = request.IdempotencyKey is { } bodyKey && bodyKey != Guid.Empty
            ? bodyKey
            : Guid.TryParse(Request.Headers["Idempotency-Key"], out var headerKey)
                ? headerKey
                : Guid.Empty;

        var command = new RecordPettyCashExpenseCommand(
            request.Amount, request.Recipient, request.Category,
            request.ReasonDetail, request.Notes, CurrentUser, idempotencyKey);

        var validation = await recordValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await recordHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}

public record RecordPettyCashRequest(
    decimal           Amount,
    string            Recipient,
    PettyCashCategory Category,
    string?           ReasonDetail,
    string?           Notes,
    /// <summary>
    /// Client-generated de-duplication token (one per submit attempt). Optional in the body
    /// if supplied via the <c>Idempotency-Key</c> header instead; one of the two is required.
    /// </summary>
    Guid?             IdempotencyKey = null);
