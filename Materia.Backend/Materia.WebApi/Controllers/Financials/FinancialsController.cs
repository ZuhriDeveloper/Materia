using Materia.Application.Financials.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Financials;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public sealed class FinancialsController(
    GetProfitAndLossQueryHandler pnlHandler,
    GetCashFlowQueryHandler      cashFlowHandler) : ControllerBase
{
    [HttpGet("pnl")]
    public async Task<IActionResult> GetProfitAndLoss(
        [FromQuery] DateTime  from,
        [FromQuery] DateTime  to,
        CancellationToken ct = default)
    {
        if (from > to)
            return BadRequest(new { message = "from must not be after to." });

        var result = await pnlHandler.HandleAsync(new GetProfitAndLossQuery(from, to), ct);
        return Ok(result);
    }

    [HttpGet("cashflow")]
    public async Task<IActionResult> GetCashFlow(
        [FromQuery] DateTime  from,
        [FromQuery] DateTime  to,
        CancellationToken ct = default)
    {
        if (from > to)
            return BadRequest(new { message = "from must not be after to." });

        var result = await cashFlowHandler.HandleAsync(new GetCashFlowQuery(from, to), ct);
        return Ok(result);
    }
}
