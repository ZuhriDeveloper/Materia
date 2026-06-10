using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Financials.RecordChangeFundDeposit;
using Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;
using Materia.Application.Financials.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Financials;

[ApiController]
[Route("api/change-fund")]
public sealed class ChangeFundController(
    RecordChangeFundDepositCommandHandler           recordHandler,
    RecordChangeFundWithdrawalCommandHandler        withdrawHandler,
    GetChangeFundQueryHandler                       getHandler,
    GetChangeFundWithdrawalsQueryHandler            getWithdrawalsHandler,
    IValidator<RecordChangeFundDepositCommand>      recordValidator,
    IValidator<RecordChangeFundWithdrawalCommand>   withdrawValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    /// <summary>
    /// Idempotency key: prefer the body field; fall back to the Idempotency-Key header.
    /// A double-clicked or retried request must reuse the same key to be de-duplicated.
    /// </summary>
    private Guid ResolveIdempotencyKey(Guid? bodyKey) =>
        bodyKey is { } k && k != Guid.Empty
            ? k
            : Guid.TryParse(Request.Headers["Idempotency-Key"], out var headerKey)
                ? headerKey
                : Guid.Empty;

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns paged list of change fund deposits and the current total balance.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Cashier")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await getHandler.HandleAsync(new GetChangeFundQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Returns paged list of change fund withdrawals (corrections).</summary>
    [HttpGet("withdrawals")]
    [Authorize(Roles = "Admin,Cashier")]
    public async Task<IActionResult> GetWithdrawals(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await getWithdrawalsHandler.HandleAsync(
            new GetChangeFundWithdrawalsQuery(page, pageSize), ct);
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Admin manually records cash deposited into the change fund.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Record(
        [FromBody] RecordChangeFundDepositRequest request,
        CancellationToken ct)
    {
        var command = new RecordChangeFundDepositCommand(
            request.Amount, request.Notes, CurrentUser,
            ResolveIdempotencyKey(request.IdempotencyKey));

        var validation = await recordValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await recordHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    /// <summary>
    /// Admin records cash taken out of the change fund — the compensating entry for
    /// correcting an erroneous deposit. The reason is mandatory (audit trail).
    /// </summary>
    [HttpPost("withdrawals")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Withdraw(
        [FromBody] RecordChangeFundWithdrawalRequest request,
        CancellationToken ct)
    {
        var command = new RecordChangeFundWithdrawalCommand(
            request.Amount, request.Reason, CurrentUser,
            ResolveIdempotencyKey(request.IdempotencyKey));

        var validation = await withdrawValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var id = await withdrawHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetWithdrawals), new { id }, new { id });
    }
}

public record RecordChangeFundDepositRequest(
    decimal Amount,
    string? Notes,
    /// <summary>
    /// Client-generated de-duplication token (one per submit attempt). Optional in the body
    /// if supplied via the <c>Idempotency-Key</c> header instead; one of the two is required.
    /// </summary>
    Guid?   IdempotencyKey = null);

public record RecordChangeFundWithdrawalRequest(
    decimal Amount,
    string  Reason,
    Guid?   IdempotencyKey = null);
