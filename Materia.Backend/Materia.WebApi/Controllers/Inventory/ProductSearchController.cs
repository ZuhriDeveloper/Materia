using Materia.Application.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Inventory;

/// <summary>
/// Lightweight product-name lookup for the PoS cashier autocomplete. Served from Redis
/// (cache-aside), and accessible to cashiers — unlike the Admin-only ProductsController.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Cashier")]
[Route("api/products/search")]
public class ProductSearchController(IProductSearchCache searchCache) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? term,
        [FromQuery] int limit = 8,
        CancellationToken ct = default)
    {
        var results = await searchCache.SearchAsync(term ?? string.Empty, Math.Clamp(limit, 1, 25), ct);
        return Ok(results);
    }
}
