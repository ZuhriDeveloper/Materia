using System.Text.Json;
using Materia.Application.Contracts.Purchasing;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Purchasing;

public class SupplierQueryRepository(AppDbContext context) : ISupplierQueryRepository
{
    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(bool activeOnly, CancellationToken ct = default)
    {
        var query = context.SupplierReadModels.AsNoTracking();
        if (activeOnly) query = query.Where(s => s.IsActive);

        var rows = await query.OrderBy(s => s.Name).ToListAsync(ct);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid supplierId, CancellationToken ct = default)
    {
        var row = await context.SupplierReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, ct);

        return row is null ? null : MapToDto(row);
    }

    public async Task<SupplierBestPriceResult?> GetBestPriceForProductAsync(
        Guid productId, CancellationToken ct = default)
    {
        var activeSuppliers = await context.SupplierReadModels
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var candidates = activeSuppliers
            .Select(s =>
            {
                var catalog = DeserializeCatalog(s.CatalogJson);
                var entry = catalog.FirstOrDefault(e => e.ProductId == productId);
                var latest = entry?.Prices.MaxBy(p => p.EffectiveFrom);
                return latest is null
                    ? null
                    : new SupplierBestPriceResult(s.Id, s.Name, latest.Amount, latest.Unit);
            })
            .OfType<SupplierBestPriceResult>()
            .ToList();

        return candidates.Count == 0 ? null : candidates.MinBy(r => r.UnitCost);
    }

    private static SupplierDto MapToDto(SupplierReadModel row)
    {
        var catalog = DeserializeCatalog(row.CatalogJson);
        return new SupplierDto(
            row.Id,
            row.Name,
            row.ContactPhone,
            row.Description,
            row.SalesmanName,
            row.SalesmanPhone,
            row.IsActive,
            catalog.Select(e => new SupplierCatalogEntryDto(
                e.ProductId,
                e.Prices.Select(p => new SupplierPriceDto(p.Amount, p.Currency, p.Unit, p.EffectiveFrom))
                        .OrderByDescending(p => p.EffectiveFrom)
                        .ToList()))
                .ToList());
    }

    private static List<CatalogEntryJson> DeserializeCatalog(string json)
        => JsonSerializer.Deserialize<List<CatalogEntryJson>>(json,
               new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
           ?? [];

    private sealed record CatalogEntryJson(Guid ProductId, List<PriceJson> Prices);
    private sealed record PriceJson(decimal Amount, string Currency, string Unit, DateTime EffectiveFrom);
}
