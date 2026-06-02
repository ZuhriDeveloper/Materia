using Materia.Application.DTOs.Sales;
using Materia.Application.Services;
using Materia.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Materia.WebApi.Controllers.Catalog;

/// <summary>
/// Public integration endpoints consumed by the Renovin AI Advisor.
/// These are [AllowAnonymous] because they are called server-to-server from
/// Renovin.ApiService — the Anthropic API key and order details never leave
/// the server tier. No PII beyond what a customer voluntarily submits is exposed.
/// </summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController(AppDbContext db, SaleService saleService) : ControllerBase
{
    // ── GET /api/catalog/search?q=Pipa&q=Lem&q=Karet&top=6 ──────────────────────
    // Accepts multiple q params — one DB roundtrip regardless of keyword count.

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string[] q,
        [FromQuery] int      top = 6,
        CancellationToken    ct  = default)
    {
        var terms = q
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        if (terms.Count == 0)
            return Ok(Array.Empty<CatalogSearchResultDto>());

        top = Math.Clamp(top, 1, 20);

        // One query: product matches ANY of the keywords (OR across all terms)
        var products = await db.ProductReadModels
            .Where(p => p.IsActive &&
                        terms.Any(t => EF.Functions.ILike(p.Name, $"%{t}%") ||
                                       EF.Functions.ILike(p.Description ?? "", $"%{t}%")))
            .OrderBy(p => p.Name)
            .Take(top)
            .ToListAsync(ct);

        if (products.Count == 0)
            return Ok(Array.Empty<CatalogSearchResultDto>());

        var productIds = products.Select(p => p.Id).ToList();

        // Stock levels
        var stocks = await db.StockReadModels
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, ct);

        // Average price from sale history (best-effort; 0 if never sold)
        var avgPrices = await db.SaleItemReadModels
            .Where(i => productIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Price = g.Average(i => i.UnitPrice) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Price, ct);

        // First category name per product
        var allCategoryIds = products
            .SelectMany(p => ParseGuids(p.CategoryIdsJson))
            .Distinct()
            .ToList();

        var categoryNames = allCategoryIds.Count > 0
            ? await db.CategoryReadModels
                .Where(c => allCategoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct)
            : new Dictionary<Guid, string>();

        var results = products.Select(p =>
        {
            stocks.TryGetValue(p.Id, out var stock);
            avgPrices.TryGetValue(p.Id, out var price);

            var firstCatId = ParseGuids(p.CategoryIdsJson).FirstOrDefault();
            var category   = firstCatId != Guid.Empty && categoryNames.TryGetValue(firstCatId, out var cn)
                ? cn : "";

            return new CatalogSearchResultDto(
                p.Id,
                p.Name,
                p.Description ?? "",
                p.BaseUnit,
                (decimal)Math.Round(price, 0),
                stock?.Quantity ?? 0m,
                category);
        }).ToList();

        return Ok(results);
    }

    // ── POST /api/catalog/customer-order ──────────────────────────────────────

    [HttpPost("customer-order")]
    [AllowAnonymous]
    public async Task<IActionResult> PlaceCustomerOrder(
        [FromBody]        CustomerOrderRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { errors = new[] { "CustomerName wajib diisi." } });

        if (request.Lines is not { Count: > 0 })
            return BadRequest(new { errors = new[] { "Minimal satu item harus dipesan." } });

        const string serviceUser = "renovin-integration";

        // 1. Create a Draft Delivery sale
        var createReq = new CreateSaleRequest(
            CustomerId:        null,
            CustomerName:      request.CustomerName.Trim(),
            SaleType:          "Delivery",
            CustomerAddressId: null,
            DeliveryAddress:   string.IsNullOrWhiteSpace(request.Notes)
                ? request.DeliveryAddress.Trim()
                : $"{request.DeliveryAddress.Trim()} — Catatan: {request.Notes.Trim()}");

        var saleId = await saleService.CreateSaleAsync(createReq, serviceUser, ct);

        // 2. Add each item
        foreach (var line in request.Lines)
        {
            var addReq = new AddSaleItemRequest(
                ProductId:   line.ProductId,
                ProductName: line.ProductName,
                UnitName:    line.UnitName,
                Quantity:    line.Quantity,
                UnitPrice:   line.UnitPrice);

            await saleService.AddItemAsync(saleId, addReq, serviceUser, ct);
        }

        // 3. Return summary (leave as Draft — Materia staff confirms and processes payment)
        var sale = await saleService.GetByIdAsync(saleId, ct);
        return Ok(new CustomerOrderResult(
            sale!.Id,
            sale.ReferenceNo,
            sale.CustomerName,
            sale.GrandTotal));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<Guid> ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try   { return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json) ?? []; }
        catch { return []; }
    }
}

// ── Renovin-facing DTOs ───────────────────────────────────────────────────────

public record CatalogSearchResultDto(
    Guid    Id,
    string  Name,
    string  Description,
    string  Unit,
    decimal Price,
    decimal Stock,
    string  Category);

public record CustomerOrderLineDto(
    Guid    ProductId,
    string  ProductName,
    string  UnitName,
    decimal Quantity,
    decimal UnitPrice);

public record CustomerOrderRequest(
    string                           CustomerName,
    string                           CustomerContact,
    string                           DeliveryAddress,
    string?                          Notes,
    IReadOnlyList<CustomerOrderLineDto> Lines);

public record CustomerOrderResult(
    Guid    Id,
    string  ReferenceNo,
    string  CustomerName,
    decimal TotalAmount);
