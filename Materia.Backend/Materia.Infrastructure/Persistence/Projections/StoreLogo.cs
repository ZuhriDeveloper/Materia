namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Stores the store logo as binary data. One row per store — replaced on upload.
/// Intentionally NOT event-sourced (binary blobs don't belong in the event store).
/// </summary>
public class StoreLogo
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public byte[] Content { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = default!;
}
