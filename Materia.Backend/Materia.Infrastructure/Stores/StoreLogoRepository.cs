using Materia.Application.Contracts.Stores;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Stores;

public class StoreLogoRepository(AppDbContext context) : IStoreLogoRepository
{
    public async Task SaveAsync(
        Guid storeId,
        byte[] content,
        string contentType,
        string fileName,
        string uploadedBy,
        CancellationToken ct = default)
    {
        // One logo per store — replace any existing one.
        var existing = await context.StoreLogos
            .Where(x => x.StoreId == storeId)
            .ToListAsync(ct);

        if (existing.Count > 0)
            context.StoreLogos.RemoveRange(existing);

        context.StoreLogos.Add(new StoreLogo
        {
            Id          = Guid.NewGuid(),
            StoreId     = storeId,
            FileName    = fileName,
            ContentType = contentType,
            Content     = content,
            UploadedAt  = DateTime.UtcNow,
            UploadedBy  = uploadedBy,
        });

        await context.SaveChangesAsync(ct);
    }

    public async Task<StoreLogoContent?> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default)
    {
        var row = await context.StoreLogos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StoreId == storeId, ct);

        return row is null ? null : new StoreLogoContent(row.Content, row.ContentType, row.FileName);
    }

    public Task<bool> ExistsAsync(Guid storeId, CancellationToken ct = default)
        => context.StoreLogos.AnyAsync(x => x.StoreId == storeId, ct);
}
