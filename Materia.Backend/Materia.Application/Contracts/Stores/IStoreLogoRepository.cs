namespace Materia.Application.Contracts.Stores;

public interface IStoreLogoRepository
{
    /// <summary>Saves or replaces the logo for the given store (one logo per store).</summary>
    Task SaveAsync(
        Guid storeId,
        byte[] content,
        string contentType,
        string fileName,
        string uploadedBy,
        CancellationToken ct = default);

    /// <summary>Returns the logo bytes and metadata, or <c>null</c> when none has been uploaded.</summary>
    Task<StoreLogoContent?> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> when the store has a logo on file.</summary>
    Task<bool> ExistsAsync(Guid storeId, CancellationToken ct = default);
}

public record StoreLogoContent(byte[] Content, string ContentType, string FileName);
